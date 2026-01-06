using _360Retail.Services.Saas.Application.DTOs.Stores;

namespace _360Retail.Services.Saas.Application.Interfaces;
public interface IStoreService
{
    Task<StoreResponseDto> CreateAsync(Guid ownerUserId, CreateStoreDto dto);
    Task<StoreResponseDto?> GetByIdAsync(Guid storeId);
    Task<List<StoreResponseDto>> GetAllAsync();
    Task<bool> UpdateAsync(Guid storeId, UpdateStoreDto dto);
    Task<bool> DeleteAsync(Guid storeId);
}
