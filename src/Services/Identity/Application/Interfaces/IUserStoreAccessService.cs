using _360Retail.Services.Identity.Application.DTOs;

namespace _360Retail.Services.Identity.Application.Interfaces;

public interface IUserStoreAccessService
{
    Task<List<UserStoreDto>> GetMyStoresAsync(Guid userId);

    Task<bool> HasStoreAccessAsync(
       Guid userId,
       Guid storeId,
       string roleInStore
   );
}
