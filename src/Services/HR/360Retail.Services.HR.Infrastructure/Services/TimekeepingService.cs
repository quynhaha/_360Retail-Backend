using _360Retail.Services.HR.Application.DTOs;
using _360Retail.Services.HR.Application.Interfaces;
using _360Retail.Services.HR.Domain.Entities;
using _360Retail.Services.HR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace _360Retail.Services.HR.Infrastructure.Services;

public class TimekeepingService : ITimekeepingService
{
    private readonly HrDbContext _db;
    private readonly ILogger<TimekeepingService> _logger;
    
    /// <summary>
    /// Bán kính chấp nhận check-in (200 mét)
    /// </summary>
    private const double MaxDistanceMeters = 200;

    public TimekeepingService(HrDbContext db, ILogger<TimekeepingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<TimekeepingDto> CheckInAsync(Guid storeId, Guid appUserId, CheckInDto dto)
    {
        // 1. Find employee
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.AppUserId == appUserId && e.StoreId == storeId && e.IsActive);

        if (employee == null)
            throw new Exception("Không tìm thấy nhân viên trong cửa hàng này");

        // 2. Check if already checked in today (Vietnam time UTC+7)
        var today = DateTime.UtcNow.AddHours(7).Date;
        var existingRecord = await _db.Timekeepings
            .FirstOrDefaultAsync(t => t.EmployeeId == employee.Id 
                                      && t.StoreId == storeId
                                      && t.CheckInTime.HasValue
                                      && t.CheckInTime.Value.AddHours(7).Date == today);

        if (existingRecord != null)
            throw new Exception("Bạn đã chấm công vào hôm nay rồi");

        // 3. GPS Geofencing check
        bool storeHasGps = false;
        if (!string.IsNullOrWhiteSpace(dto.LocationGps))
        {
            storeHasGps = await ValidateGpsDistance(storeId, dto.LocationGps);
        }
        else
        {
            var coords = await _db.Database.SqlQueryRaw<StoreGps>(
                "SELECT latitude AS \"Latitude\", longitude AS \"Longitude\" FROM saas.stores WHERE id = {0}",
                storeId).FirstOrDefaultAsync();
            storeHasGps = coords?.Latitude != null && coords?.Longitude != null;
        }

        // 4. Determine if late (after 9:00 AM Vietnam time = 2:00 AM UTC)
        var now = DateTime.UtcNow;
        var vnNow = now.AddHours(7); // Convert to Vietnam timezone (UTC+7)
        var lateThreshold = vnNow.Date.AddHours(9); // 9:00 AM Vietnam
        var isLate = vnNow > lateThreshold;

        // 5. Create timekeeping record
        var timekeeping = new Timekeeping
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            StoreId = storeId,
            CheckInTime = now,
            LocationGps = dto.LocationGps,
            CheckInImageUrl = dto.CheckInImageUrl,
            IsLate = isLate
        };

        _db.Timekeepings.Add(timekeeping);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Employee {EmployeeName} checked in at {CheckInTime}, isLate={IsLate}",
            employee.FullName, now, isLate);

        var result = MapToDto(timekeeping, employee.FullName);
        if (!storeHasGps)
            result.Warning = "⚠️ Cửa hàng chưa cài đặt tọa độ GPS. Vui lòng cập nhật địa chỉ cửa hàng trên bản đồ trong Cài đặt để sử dụng chấm công GPS chính xác.";

        return result;
    }

    public async Task<TimekeepingDto> CheckOutAsync(Guid storeId, Guid appUserId, CheckOutDto dto)
    {
        // 1. Find employee
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.AppUserId == appUserId && e.StoreId == storeId && e.IsActive);

        if (employee == null)
            throw new Exception("Không tìm thấy nhân viên trong cửa hàng này");

        // 2. Find today's check-in record
        var today = DateTime.UtcNow.Date;
        var record = await _db.Timekeepings
            .FirstOrDefaultAsync(t => t.EmployeeId == employee.Id
                                      && t.StoreId == storeId
                                      && t.CheckInTime.HasValue
                                      && t.CheckInTime.Value.Date == today
                                      && !t.CheckOutTime.HasValue);

        if (record == null)
            throw new Exception("Bạn chưa chấm công vào hôm nay hoặc đã chấm công ra rồi");

        // 3. GPS check on checkout too (optional)
        if (!string.IsNullOrWhiteSpace(dto.LocationGps))
        {
            await ValidateGpsDistance(storeId, dto.LocationGps);
        }

        // 4. Update checkout time
        record.CheckOutTime = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Employee {EmployeeName} checked out at {CheckOutTime}",
            employee.FullName, record.CheckOutTime);

        return MapToDto(record, employee.FullName);
    }

    public async Task<TodayStatusDto> GetTodayStatusAsync(Guid storeId, Guid appUserId)
    {
        // Check store GPS config
        var storeCoords = await _db.Database.SqlQueryRaw<StoreGps>(
            "SELECT latitude AS \"Latitude\", longitude AS \"Longitude\" FROM saas.stores WHERE id = {0}",
            storeId).FirstOrDefaultAsync();
        bool isGpsConfigured = storeCoords?.Latitude != null && storeCoords?.Longitude != null;
        string? gpsWarning = isGpsConfigured ? null 
            : "⚠️ Cửa hàng chưa cài đặt tọa độ GPS. Vui lòng cập nhật địa chỉ trong Cài đặt để sử dụng chấm công GPS.";

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.AppUserId == appUserId && e.StoreId == storeId);

        if (employee == null)
            return new TodayStatusDto 
            { 
                HasCheckedIn = false, HasCheckedOut = false,
                IsGpsConfigured = isGpsConfigured, Warning = gpsWarning
            };

        var today = DateTime.UtcNow.Date;
        var record = await _db.Timekeepings
            .FirstOrDefaultAsync(t => t.EmployeeId == employee.Id
                                      && t.StoreId == storeId
                                      && t.CheckInTime.HasValue
                                      && t.CheckInTime.Value.Date == today);

        return new TodayStatusDto
        {
            HasCheckedIn = record != null,
            HasCheckedOut = record?.CheckOutTime.HasValue ?? false,
            IsGpsConfigured = isGpsConfigured,
            Warning = gpsWarning,
            Record = record != null ? MapToDto(record, employee.FullName) : null
        };
    }

    public async Task<List<TimekeepingDto>> GetHistoryAsync(
        Guid storeId, Guid? employeeId, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var query = _db.Timekeepings
            .Include(t => t.Employee)
            .Where(t => t.StoreId == storeId);

        if (employeeId.HasValue)
            query = query.Where(t => t.EmployeeId == employeeId.Value);

        if (from.HasValue)
            query = query.Where(t => t.CheckInTime >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.CheckInTime <= to.Value.Date.AddDays(1));

        var records = await query
            .OrderByDescending(t => t.CheckInTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return records.Select(r => MapToDto(r, r.Employee?.FullName)).ToList();
    }

    public async Task<List<TimekeepingSummaryDto>> GetSummaryAsync(Guid storeId, int month, int year)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var records = await _db.Timekeepings
            .Include(t => t.Employee)
            .Where(t => t.StoreId == storeId
                        && t.CheckInTime >= startDate
                        && t.CheckInTime < endDate)
            .ToListAsync();

        var grouped = records
            .GroupBy(r => new { r.EmployeeId, r.Employee?.FullName })
            .Select(g =>
            {
                var completedRecords = g.Where(r => r.CheckInTime.HasValue && r.CheckOutTime.HasValue).ToList();
                var totalHours = completedRecords
                    .Sum(r => (r.CheckOutTime!.Value - r.CheckInTime!.Value).TotalHours);
                var totalDays = g.Select(r => r.CheckInTime?.Date).Distinct().Count();

                return new TimekeepingSummaryDto
                {
                    EmployeeId = g.Key.EmployeeId,
                    EmployeeName = g.Key.FullName,
                    TotalDays = totalDays,
                    LateDays = g.Count(r => r.IsLate == true),
                    TotalHours = Math.Round(totalHours, 1),
                    AvgHoursPerDay = totalDays > 0 ? Math.Round(totalHours / totalDays, 1) : 0
                };
            })
            .ToList();

        return grouped;
    }

    #region Private Helpers

    /// <summary>
    /// Validate GPS distance between user and store using Haversine formula
    /// </summary>
    /// <returns>true if store has GPS configured, false otherwise</returns>
    private async Task<bool> ValidateGpsDistance(Guid storeId, string locationGps)
    {
        // Parse user GPS
        var parts = locationGps.Split(',');
        if (parts.Length != 2 
            || !double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out var userLat)
            || !double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out var userLng))
        {
            _logger.LogWarning("Invalid GPS format: {LocationGps}", locationGps);
            throw new Exception("Tọa độ GPS không hợp lệ. Format: 'latitude,longitude' (ví dụ: '10.7769,106.7009')");
        }

        // Get store coordinates (raw SQL since Store is in SaaS DbContext)
        var storeCoords = await _db.Database.SqlQueryRaw<StoreGps>(
            "SELECT latitude AS \"Latitude\", longitude AS \"Longitude\" FROM saas.stores WHERE id = {0}", 
            storeId)
            .FirstOrDefaultAsync();

        if (storeCoords?.Latitude == null || storeCoords?.Longitude == null)
        {
            _logger.LogDebug("Store {StoreId} has no GPS coordinates, skipping geofencing", storeId);
            return false; // Store chưa cài GPS
        }

        var distance = CalculateHaversineDistance(
            userLat, userLng, 
            storeCoords.Latitude!.Value, storeCoords.Longitude!.Value);

        _logger.LogDebug("GPS distance: {Distance}m (max: {MaxDistance}m)", 
            Math.Round(distance), MaxDistanceMeters);

        if (distance > MaxDistanceMeters)
        {
            throw new Exception(
                $"Bạn đang ở quá xa cửa hàng ({Math.Round(distance)}m). " +
                $"Khoảng cách tối đa cho phép là {MaxDistanceMeters}m.");
        }

        return true; // Store has GPS configured
    }

    /// <summary>
    /// Haversine formula to calculate distance between two GPS coordinates in meters
    /// </summary>
    private static double CalculateHaversineDistance(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371000; // Earth's radius in meters
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    private static TimekeepingDto MapToDto(Timekeeping t, string? employeeName)
    {
        double? workHours = null;
        if (t.CheckInTime.HasValue && t.CheckOutTime.HasValue)
            workHours = Math.Round((t.CheckOutTime.Value - t.CheckInTime.Value).TotalHours, 1);

        return new TimekeepingDto
        {
            Id = t.Id,
            EmployeeId = t.EmployeeId,
            EmployeeName = employeeName,
            CheckInTime = t.CheckInTime,
            CheckOutTime = t.CheckOutTime,
            LocationGps = t.LocationGps,
            CheckInImageUrl = t.CheckInImageUrl,
            IsLate = t.IsLate ?? false,
            WorkHours = workHours
        };
    }

    /// <summary>
    /// Helper class for raw SQL query to get store coordinates
    /// </summary>
    private class StoreGps
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    #endregion
}
