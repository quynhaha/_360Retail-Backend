namespace _360Retail.Services.HR.Application.DTOs;

/// <summary>
/// Internal DTO - Identity calls HR to create employee after invite
/// </summary>
public class CreateEmployeeDto
{
    public Guid AppUserId { get; set; }
    public Guid StoreId { get; set; }
    public string Email { get; set; } = null!;  // Use as initial FullName
    public string Role { get; set; } = "Staff"; // Position: Staff/Manager
}
