namespace _360Retail.Services.Identity.Application.DTOs.SuperAdmin;

public class UpdateUserDto
{
    public bool IsActivated { get; set; }
    public string Status { get; set; } = "Active";
}
