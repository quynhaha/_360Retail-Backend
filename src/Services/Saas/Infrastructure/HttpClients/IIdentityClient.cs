namespace _360Retail.Services.Saas.Infrastructure.HttpClients;

public interface IIdentityClient
{
    Task AssignStoreAsync(string accessToken, Guid storeId);
    Task<bool> HasStoreAccessAsync(string accessToken, Guid storeId, string roleInStore);
    Task<List<UserStoreAccessDto>> GetMyStoresAsync(string accessToken);
}

public class UserStoreAccessDto
{
    public Guid StoreId { get; set; }
    public string RoleInStore { get; set; } = null!;
    public bool IsDefault { get; set; }
}
