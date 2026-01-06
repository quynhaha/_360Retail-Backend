namespace _360Retail.Services.Identity.Application.DTOs.SuperAdmin;

public class CreateUserDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    // StoreOwner | Manager | Staff
    public string RoleName { get; set; } = null!;
}
