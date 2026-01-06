namespace _360Retail.Services.Identity.Application.DTOs.SuperAdmin;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public bool IsActivated { get; set; }
    public string Status { get; set; } = null!;
    public Guid? StoreId { get; set; }
    public List<string> Roles { get; set; } = new();
}
