using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _360Retail.Services.CRM.Domain.Entities;

namespace _360Retail.Services.CRM.Application.Interfaces;

public interface ILoyaltyRuleRepository
{
    Task<LoyaltyRule?> GetByIdAsync(Guid id);
    Task<IEnumerable<LoyaltyRule>> GetByStoreIdAsync(Guid storeId);
    Task AddAsync(LoyaltyRule rule);
    Task UpdateAsync(LoyaltyRule rule);
    Task DeleteAsync(Guid id); // Soft delete
}

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id);
    Task UpdateAsync(Customer customer);
}

public interface ILoyaltyTransactionRepository
{
    Task AddAsync(LoyaltyTransaction transaction);
    Task<IEnumerable<LoyaltyTransaction>> GetByCustomerIdAsync(Guid customerId, int page, int pageSize);
    Task<int> GetTotalCountAsync(Guid customerId);
    Task<bool> ExistsByOrderIdAsync(Guid orderId);
}

public interface IIdempotencyRepository
{
    Task<bool> ExistsAsync(string key);
    Task AddAsync(string key, int statusCode, string responseBody, TimeSpan expiry);
}
