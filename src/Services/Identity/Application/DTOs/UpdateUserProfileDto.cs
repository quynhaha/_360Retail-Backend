namespace _360Retail.Services.Identity.Application.DTOs;

/// <summary>
/// DTO for HR to update user profile (partial update)
/// </summary>
public class UpdateUserProfileDto
{
    public string? UserName { get; set; }
    public string? PhoneNumber { get; set; }
}
