namespace _360Retail.Services.Saas.Infrastructure.HttpClients;

public interface IIdentityClient
{
    Task AssignStoreAsync(string accessToken, Guid storeId);
    Task<bool> HasStoreAccessAsync( string accessToken, Guid storeId, string roleInStore);
}
