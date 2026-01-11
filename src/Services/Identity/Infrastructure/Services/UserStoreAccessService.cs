using Microsoft.EntityFrameworkCore;

using _360Retail.Services.Identity.Application.DTOs;
using _360Retail.Services.Identity.Application.Interfaces;
using _360Retail.Services.Identity.Infrastructure.Persistence;

namespace _360Retail.Services.Identity.Infrastructure.Services.UserStoreAccess;

public class UserStoreAccessService : IUserStoreAccessService
{
    private readonly IdentityDbContext _db;

    public UserStoreAccessService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<List<UserStoreDto>> GetMyStoresAsync(Guid userId)
    {
        return await _db.UserStoreAccess
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new UserStoreDto
            {
                StoreId = x.StoreId,
                RoleInStore = x.RoleInStore,
                IsDefault = x.IsDefault
            })
            .ToListAsync();
    }
}
