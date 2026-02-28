namespace _360Retail.Services.HR.Application.Interfaces;

public interface ITimekeepingService
{
    /// <summary>
    /// Employee check-in (with optional GPS geofencing)
    /// </summary>
    Task<DTOs.TimekeepingDto> CheckInAsync(Guid storeId, Guid appUserId, DTOs.CheckInDto dto);

    /// <summary>
    /// Employee check-out
    /// </summary>
    Task<DTOs.TimekeepingDto> CheckOutAsync(Guid storeId, Guid appUserId, DTOs.CheckOutDto dto);

    /// <summary>
    /// Get today's check-in/out status for current user
    /// </summary>
    Task<DTOs.TodayStatusDto> GetTodayStatusAsync(Guid storeId, Guid appUserId);

    /// <summary>
    /// Get timekeeping history (Manager/Owner: all employees, Staff: own only)
    /// </summary>
    Task<List<DTOs.TimekeepingDto>> GetHistoryAsync(Guid storeId, Guid? employeeId, DateTime? from, DateTime? to, int page, int pageSize);

    /// <summary>
    /// Get monthly summary per employee (Manager/Owner only)
    /// </summary>
    Task<List<DTOs.TimekeepingSummaryDto>> GetSummaryAsync(Guid storeId, int month, int year);
}
