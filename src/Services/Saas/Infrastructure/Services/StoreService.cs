using _360Retail.Services.Saas.Application.Interfaces;
using _360Retail.Services.Saas.Application.DTOs.Stores;
using _360Retail.Services.Saas.Domain.Entities;
using _360Retail.Services.Saas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace _360Retail.Services.Saas.Infrastructure.Services;

public class StoreService : IStoreService
{
    private readonly SaasDbContext _db;

    public StoreService(SaasDbContext db)
    {
        _db = db;
    }

    // CREATE
    public async Task<StoreResponseDto> CreateAsync(Guid ownerUserId, CreateStoreDto dto)
    {
        var store = new Store
        {
            Id = Guid.NewGuid(),
            StoreName = dto.StoreName,
            Address = dto.Address,
            Phone = dto.Phone,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Stores.Add(store);
        await _db.SaveChangesAsync();

        return MapToDto(store);
    }

    // READ ONE
    public async Task<StoreResponseDto?> GetByIdAsync(Guid storeId)
    {
        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == storeId);
        return store == null ? null : MapToDto(store);
    }

    // READ ALL
    public async Task<List<StoreResponseDto>> GetAllAsync()
    {
        return await _db.Stores
            .Select(s => MapToDto(s))
            .ToListAsync();
    }

    // UPDATE
    public async Task<bool> UpdateAsync(Guid storeId, UpdateStoreDto dto)
    {
        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == storeId);
        if (store == null) return false;

        store.StoreName = dto.StoreName;
        store.Address = dto.Address;
        store.Phone = dto.Phone;
        store.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return true;
    }

    // DELETE (SOFT DELETE)
    public async Task<bool> DeleteAsync(Guid storeId)
    {
        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == storeId);
        if (store == null) return false;

        store.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    private static StoreResponseDto MapToDto(Store s)
    {
        return new StoreResponseDto
        {
            Id = s.Id,
            StoreName = s.StoreName,
            Address = s.Address,
            Phone = s.Phone,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt
        };
    }
}
