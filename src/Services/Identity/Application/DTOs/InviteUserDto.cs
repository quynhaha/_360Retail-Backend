namespace _360Retail.Services.Identity.Application.DTOs;

public class InviteUserDto
{
    public string Email { get; set; } = null!;
    public Guid StoreId { get; set; }

    public string Role { get; set; } = "Staff";
}
