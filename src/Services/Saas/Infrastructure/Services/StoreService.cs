using _360Retail.Services.Saas.Application.Interfaces;
using _360Retail.Services.Saas.Application.DTOs.Stores;
using _360Retail.Services.Saas.Domain.Entities;
using _360Retail.Services.Saas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

    // CREATE WITH SUBSCRIPTION (for paid users creating new stores)
    public async Task<CreateStoreWithSubscriptionResult> CreateWithSubscriptionAsync(Guid ownerUserId, CreateStoreDto dto)
    {
        // === PLAN LIMIT: has_multi_store ===
        // Check if user already has a store → if so, check if plan allows multi-store
        var existingStoreCount = await _db.Stores.CountAsync(s => s.IsActive);
        if (existingStoreCount > 0)
        {
            // Get features from any existing active subscription for the user's stores
            var existingSub = await _db.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.Status == "Active" || s.Status == "Trial")
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();

            if (existingSub?.Plan?.Features != null)
            {
                try
                {
                    var features = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(existingSub.Plan.Features);
                    if (features != null && features.TryGetValue("has_multi_store", out var multiStore) && multiStore.ValueKind == JsonValueKind.False)
                        throw new Exception("Tính năng quản lý đa cửa hàng không khả dụng trong gói hiện tại. Vui lòng nâng cấp lên gói Pro hoặc cao hơn.");
                }
                catch (JsonException) { /* ignore parse errors */ }
            }
        }

        // Create the store first
        var store = new Store
        {
            Id = Guid.NewGuid(),
            StoreName = dto.StoreName,
            Address = dto.Address,
            Phone = dto.Phone,
            IsActive = false,  // Inactive until subscription is paid
            CreatedAt = DateTime.UtcNow
        };

        _db.Stores.Add(store);

        // If PlanId is provided, create pending subscription
        if (dto.PlanId.HasValue)
        {
            var plan = await _db.ServicePlans.FindAsync(dto.PlanId.Value);
            if (plan == null)
                throw new Exception("Không tìm thấy gói dịch vụ");

            if (plan.IsActive != true)
                throw new Exception("Gói dịch vụ không khả dụng");

            // Create pending subscription
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                StoreId = store.Id,
                PlanId = dto.PlanId.Value,
                Status = "Pending",
                AutoRenew = false
            };

            _db.Subscriptions.Add(subscription);

            // Create pending payment
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscription.Id,
                Amount = plan.Price,
                PaymentMethod = "VNPay",
                Status = "Pending",
                Provider = "VNPay",
                PaymentDate = DateTime.UtcNow,
                UserId = ownerUserId
            };

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            // Build VNPay payment URL (simplified - actual URL building done by VNPayService)
            var paymentUrl = $"/api/payments/initiate?paymentId={payment.Id}";

            return new CreateStoreWithSubscriptionResult(
                MapToDto(store),
                paymentUrl,
                payment.Id,
                plan.Price,
                plan.PlanName
            );
        }

        await _db.SaveChangesAsync();
        return new CreateStoreWithSubscriptionResult(MapToDto(store));
    }

    // CREATE TRIAL STORE (with trial subscription)
    public async Task<Store> CreateTrialStoreAsync(string storeName)
    {
        var store = new Store
        {
            Id = Guid.NewGuid(),
            StoreName = storeName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Stores.Add(store);

        // Find or create Trial plan
        var trialPlan = await _db.ServicePlans.FirstOrDefaultAsync(p => p.PlanName == "Trial");
        
        if (trialPlan != null)
        {
            // Create trial subscription
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                StoreId = store.Id,
                PlanId = trialPlan.Id,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7),
                Status = "Trial",
                AutoRenew = false
            };
            _db.Subscriptions.Add(subscription);
        }

        await _db.SaveChangesAsync();

        return store;
    }

    // READ ONE
    public async Task<StoreResponseDto?> GetByIdAsync(Guid storeId, bool includeInactive = false)
    {
        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == storeId && (includeInactive || s.IsActive));
        return store == null ? null : MapToDto(store);
    }

    // READ ALL
    public async Task<List<StoreResponseDto>> GetAllAsync(bool includeInactive = false)
    {
        return await _db.Stores
            .Where(s => includeInactive || s.IsActive)
            .Select(s => MapToDto(s))
            .ToListAsync();
    }

    // READ BY IDS
    public async Task<List<StoreResponseDto>> GetByIdsAsync(List<Guid> storeIds, bool includeInactive = false)
    {
        return await _db.Stores
            .Where(s => storeIds.Contains(s.Id) && (includeInactive || s.IsActive))
            .Select(s => MapToDto(s))
            .ToListAsync();
    }

    // UPDATE (PARTIAL UPDATE - only update non-null/non-empty fields)
    public async Task<bool> UpdateAsync(Guid storeId, UpdateStoreDto dto)
    {
        var store = await _db.Stores.FirstOrDefaultAsync(s => s.Id == storeId);
        if (store == null) return false;

        // Only update fields that are provided (not null or empty)
        if (!string.IsNullOrWhiteSpace(dto.StoreName))
            store.StoreName = dto.StoreName;
        
        if (!string.IsNullOrWhiteSpace(dto.Address))
            store.Address = dto.Address;
        
        if (!string.IsNullOrWhiteSpace(dto.Phone))
            store.Phone = dto.Phone;
        
        if (dto.IsActive.HasValue)
            store.IsActive = dto.IsActive.Value;

        // GPS coordinates — must provide both or neither
        if (dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            store.Latitude = dto.Latitude.Value;
            store.Longitude = dto.Longitude.Value;
        }
        else if (dto.Latitude.HasValue || dto.Longitude.HasValue)
        {
            throw new Exception("Phải cung cấp cả latitude và longitude cùng lúc");
        }

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
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt
        };
    }
}
