using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.HR.Application.DTOs;

// ===== Request DTOs =====

/// <summary>
/// DTO for employee check-in
/// </summary>
public class CheckInDto
{
    /// <summary>
    /// GPS coordinates "latitude,longitude" (e.g. "10.7769,106.7009")
    /// </summary>
    public string? LocationGps { get; set; }

    /// <summary>
    /// Optional selfie/photo URL for check-in verification
    /// </summary>
    public string? CheckInImageUrl { get; set; }
}

/// <summary>
/// DTO for employee check-out
/// </summary>
public class CheckOutDto
{
    /// <summary>
    /// GPS coordinates "latitude,longitude"
    /// </summary>
    public string? LocationGps { get; set; }
}

// ===== Response DTOs =====

public class TimekeepingDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? LocationGps { get; set; }
    public string? CheckInImageUrl { get; set; }
    public bool IsLate { get; set; }
    
    /// <summary>
    /// Computed work hours (CheckOut - CheckIn)
    /// </summary>
    public double? WorkHours { get; set; }

    /// <summary>
    /// Warning message (e.g. GPS not configured)
    /// </summary>
    public string? Warning { get; set; }
}

public class TimekeepingSummaryDto
{
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public int TotalDays { get; set; }
    public int LateDays { get; set; }
    public double TotalHours { get; set; }
    public double AvgHoursPerDay { get; set; }
}

public class TodayStatusDto
{
    public bool HasCheckedIn { get; set; }
    public bool HasCheckedOut { get; set; }
    public bool IsGpsConfigured { get; set; }
    public string? Warning { get; set; }
    public TimekeepingDto? Record { get; set; }
}
