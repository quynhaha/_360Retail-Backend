namespace _360Retail.Services.Identity.Application.DTOs;

public class UserStoreDto
{
    public Guid StoreId { get; set; }
    public string RoleInStore { get; set; } = null!;
    public bool IsDefault { get; set; }
}
