using _360Retail.Services.Saas.Application.DTOs.Stores;
using _360Retail.Services.Saas.Domain.Entities;

namespace _360Retail.Services.Saas.Application.Interfaces;
public interface IStoreService
{
    Task<StoreResponseDto> CreateAsync(Guid ownerUserId, CreateStoreDto dto);
    Task<Store> CreateTrialStoreAsync(string storeName);
    Task<StoreResponseDto?> GetByIdAsync(Guid storeId, bool includeInactive = false);
    Task<List<StoreResponseDto>> GetByIdsAsync(List<Guid> storeIds, bool includeInactive = false);
    Task<List<StoreResponseDto>> GetAllAsync(bool includeInactive = false);
    Task<bool> UpdateAsync(Guid storeId, UpdateStoreDto dto);
    Task<bool> DeleteAsync(Guid storeId);
}

