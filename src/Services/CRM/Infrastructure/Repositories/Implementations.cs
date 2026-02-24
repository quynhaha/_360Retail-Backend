using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using _360Retail.Services.CRM.Domain.Entities;
using _360Retail.Services.CRM.Infrastructure.Persistence;
using _360Retail.Services.CRM.Application.Interfaces;

namespace _360Retail.Services.CRM.Infrastructure.Repositories;

public class LoyaltyRuleRepository : ILoyaltyRuleRepository
{
    private readonly CrmDbContext _context;

    public LoyaltyRuleRepository(CrmDbContext context)
    {
        _context = context;
    }

    public async Task<LoyaltyRule?> GetByIdAsync(Guid id)
    {
        return await _context.LoyaltyRules
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
    }

    public async Task<IEnumerable<LoyaltyRule>> GetByStoreIdAsync(Guid storeId)
    {
        return await _context.LoyaltyRules
            .Where(r => r.StoreId == storeId && !r.IsDeleted)
            .ToListAsync();
    }

    public async Task AddAsync(LoyaltyRule rule)
    {
        await _context.LoyaltyRules.AddAsync(rule);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(LoyaltyRule rule)
    {
        rule.UpdatedAt = DateTime.UtcNow;
        _context.LoyaltyRules.Update(rule);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var rule = await GetByIdAsync(id);
        if (rule != null)
        {
            rule.IsDeleted = true;
            rule.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}

public class CustomerRepository : ICustomerRepository
{
    private readonly CrmDbContext _context;

    public CustomerRepository(CrmDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(Guid id)
    {
        return await _context.Customers.FindAsync(id);
    }

    public async Task UpdateAsync(Customer customer)
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
    }
}

public class LoyaltyTransactionRepository : ILoyaltyTransactionRepository
{
    private readonly CrmDbContext _context;

    public LoyaltyTransactionRepository(CrmDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(LoyaltyTransaction transaction)
    {
        await _context.LoyaltyTransactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<LoyaltyTransaction>> GetByCustomerIdAsync(Guid customerId, int page, int pageSize)
    {
        return await _context.LoyaltyTransactions
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.TransactionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(Guid customerId)
    {
        return await _context.LoyaltyTransactions
            .CountAsync(t => t.CustomerId == customerId);
    }

    public async Task<bool> ExistsByOrderIdAsync(Guid orderId)
    {
        return await _context.LoyaltyTransactions.AnyAsync(t => t.OrderId == orderId);
    }
}

// Simple in-memory or DB implementation for idempotency
public class IdempotencyRepository : IIdempotencyRepository
{
    // In a real production scenario, use Redis or a DB table 'IdempotencyRecords'
    // For this example, we mock it or assume a DB table exists if we added it to DbSet
    // Let's implement using the DB Context assuming DbSet is there (we will add it)
    private readonly CrmDbContext _context;

    public IdempotencyRepository(CrmDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _context.IdempotencyRecords.AnyAsync(x => x.Key == key);
    }

    public async Task AddAsync(string key, int statusCode, string responseBody, TimeSpan expiry)
    {
        var record = new IdempotencyRecord
        {
            Key = key,
            StatusCode = statusCode,
            ResponseBody = responseBody,
            ExpiresAt = DateTime.UtcNow.Add(expiry)
        };
        await _context.IdempotencyRecords.AddAsync(record);
        await _context.SaveChangesAsync();
    }
}
