namespace _360Retail.Services.HR.Application.DTOs;

/// <summary>
/// DTO for employees to update their own profile (partial update)
/// </summary>
public class UpdateEmployeeProfileDto
{
    // HR Service handles
    public string? FullName { get; set; }
    
    // Identity Service handles (will call Identity API)
    public string? UserName { get; set; }
    public string? PhoneNumber { get; set; }
}
