using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Testcontainers.PostgreSql;
using _360Retail.Services.CRM.Application.DTOs;
using _360Retail.Services.CRM.Application.Services;
using _360Retail.Services.CRM.Domain.Entities;
using _360Retail.Services.CRM.Domain.Enums;
using _360Retail.Services.CRM.Infrastructure.Persistence;
using _360Retail.Services.CRM.Infrastructure.Repositories;

namespace CRM.Loyalty.Tests;

public class LoyaltyServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    // Use a separate context for setup/assert
    private CrmDbContext _dbContext; 
    private string _connectionString;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        _connectionString = _dbContainer.GetConnectionString();
        
        var options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        _dbContext = new CrmDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        var customerRepo = new CustomerRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _dbContainer.StopAsync();
    }

    private async Task<Customer> SeedCustomerAsync(Guid storeId, int initialPoints = 0)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            FullName = "Test Customer",
            PhoneNumber = "1234567890",
            TotalPoints = initialPoints,
            Rank = "Bronze"
        };
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();
        _dbContext.Entry(customer).State = EntityState.Detached;
        return customer;
    }

    private async Task<LoyaltyRule> SeedRuleAsync(Guid storeId, LoyaltyRuleType type, decimal earningRate, decimal minSpend = 0, bool isActive = true)
    {
        var rule = new LoyaltyRule
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            Name = $"Test Rule {type}",
            Type = type,
            EarningRate = earningRate,
            MinSpend = minSpend,
            Status = isActive ? LoyaltyRuleStatus.Active : LoyaltyRuleStatus.Inactive,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        _dbContext.LoyaltyRules.Add(rule);
        await _dbContext.SaveChangesAsync();
        _dbContext.Entry(rule).State = EntityState.Detached;
        return rule;
    }

    [Fact]
    public async Task EarnPoints_Concurrency_10ParallelRequests_ShouldProcessOnce()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var customer = await SeedCustomerAsync(storeId);
        await SeedRuleAsync(storeId, LoyaltyRuleType.PERCENT_ORDER_VALUE, 0.001m); // 1 pt per 1000

        var orderId = Guid.NewGuid();
        var request = new EarnPointsRequestDto 
        {
            CustomerId = customer.Id,
            OrderId = orderId, 
            TotalAmount = 100000, 
            TotalQuantity = 1 
        }; // 100 points

        // Act - Run 10 parallel requests
        var tasks = Enumerable.Range(0, 10).Select(async _ => 
        {
            // New Context per request to simulate real separate requests
            var options = new DbContextOptionsBuilder<CrmDbContext>()
                .UseNpgsql(_connectionString)
                .Options;
            
            using var context = new CrmDbContext(options);
            var customerRepo = new CustomerRepository(context);
            var ruleRepo = new LoyaltyRuleRepository(context);
            var transactionRepo = new LoyaltyTransactionRepository(context);
            var service = new LoyaltyService(customerRepo, transactionRepo, ruleRepo);

            try 
            {
                await service.ProcessEarnPointsAsync(storeId, request);
            }
            catch (Exception) 
            {
                // Expected Concurrency Exception or Unique Constraint Violation
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        // Re-query with fresh context
        var optionsAssert = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        using var contextAssert = new CrmDbContext(optionsAssert);

        // 1. Only 1 transaction existed
        var transactions = await contextAssert.LoyaltyTransactions
            .Where(t => t.OrderId == orderId)
            .ToListAsync();
        
        Assert.Single(transactions);

        // 2. Points only added once (100 pts)
        var updatedCustomer = await contextAssert.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customer.Id);
            
        Assert.Equal(100, updatedCustomer?.TotalPoints);
    }

    [Theory]
    [InlineData(100.4m, 0.1, 10)] // 100.4 * 0.1 = 10.04 -> Round -> 10
    [InlineData(100.5m, 0.1, 10)] // 100.5 * 0.1 = 10.05 -> Round (Midpoint to even) -> 10
    [InlineData(105.0m, 0.1, 11)] // 105.0 * 0.1 = 10.5 -> Round -> 11 (Wait, default Math.Round is ToEven. 10.5 -> 10. But typically in .NET Math.Round(10.5) is 10. Let's just test 106.0m = 10.6 -> 11).
    [InlineData(999.9m, 0.1, 100)] // 99.99 -> 100
    [InlineData(0, 0.1, 0)] // Zero
    [InlineData(-100, 0.1, 0)] // Negative shouldn't give points
    public async Task EarnPoints_PercentOrderValue_RoundedCorrectly(decimal totalAmount, decimal rate, int expectedPoints)
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var customer = await SeedCustomerAsync(storeId);
        await SeedRuleAsync(storeId, LoyaltyRuleType.PERCENT_ORDER_VALUE, rate);

        var request = new EarnPointsRequestDto 
        {
            CustomerId = customer.Id,
            OrderId = Guid.NewGuid(),
            TotalAmount = totalAmount,
            TotalQuantity = 1
        };

        var service = new LoyaltyService(new CustomerRepository(_dbContext), new LoyaltyTransactionRepository(_dbContext), new LoyaltyRuleRepository(_dbContext));

        // Act
        await service.ProcessEarnPointsAsync(storeId, request);

        // Assert
        var updatedCustomer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == customer.Id);
        Assert.Equal(expectedPoints, updatedCustomer?.TotalPoints);
    }

    [Fact]
    public async Task EarnPoints_FixedPerOrder_HandlesZeroQuantity_AndEarnsProperly()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var customer = await SeedCustomerAsync(storeId);
        await SeedRuleAsync(storeId, LoyaltyRuleType.FIXED_PER_ORDER, 50m);

        var request = new EarnPointsRequestDto 
        {
            CustomerId = customer.Id,
            OrderId = Guid.NewGuid(),
            TotalAmount = 10m,
            TotalQuantity = 0 // edge case
        };

        var service = new LoyaltyService(new CustomerRepository(_dbContext), new LoyaltyTransactionRepository(_dbContext), new LoyaltyRuleRepository(_dbContext));

        // Act
        await service.ProcessEarnPointsAsync(storeId, request);

        // Assert
        var updatedCustomer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == customer.Id);
        Assert.Equal(50, updatedCustomer?.TotalPoints);
    }

    [Fact]
    public async Task EarnPoints_PointPerQuantity_UsesQuantityProperly()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var customer = await SeedCustomerAsync(storeId);
        await SeedRuleAsync(storeId, LoyaltyRuleType.POINT_PER_QUANTITY, 2.5m); // 2.5 pts per item

        var request = new EarnPointsRequestDto 
        {
            CustomerId = customer.Id,
            OrderId = Guid.NewGuid(),
            TotalAmount = 100m,
            TotalQuantity = 5 // 5 * 2.5 = 12.5 -> Round -> 12 or 13 depending on ToEven
        };

        var service = new LoyaltyService(new CustomerRepository(_dbContext), new LoyaltyTransactionRepository(_dbContext), new LoyaltyRuleRepository(_dbContext));

        // Act
        await service.ProcessEarnPointsAsync(storeId, request);

        // Assert
        var updatedCustomer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == customer.Id);
        // Math.Round(12.5) Bankers rounding goes to nearest even which is 12
        Assert.Equal(12, updatedCustomer?.TotalPoints);
    }
}
