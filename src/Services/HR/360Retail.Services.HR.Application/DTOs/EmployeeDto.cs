namespace _360Retail.Services.HR.Application.DTOs;

/// <summary>
/// Response DTO for employee profile
/// </summary>
public class EmployeeDto
{
    public Guid Id { get; set; }
    public Guid AppUserId { get; set; }
    public Guid StoreId { get; set; }
    public string FullName { get; set; } = null!;
    public string? Position { get; set; }
    
    // From Identity Service
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    
    public decimal? BaseSalary { get; set; }
    public DateTime? JoinDate { get; set; }
    public bool IsActive { get; set; }
    public string? AvatarUrl { get; set; }
}
