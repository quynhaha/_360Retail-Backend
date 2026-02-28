using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using _360Retail.Services.Saas.Domain.Entities;
using _360Retail.Services.Saas.Infrastructure.Persistence;
using _360Retail.Services.Saas.Infrastructure.Services;

namespace Saas.Subscription.Tests;

public class SubscriptionServiceTests : IDisposable
{
    private readonly SaasDbContext _db;
    private readonly SubscriptionService _sut;

    public SubscriptionServiceTests()
    {
        var options = new DbContextOptionsBuilder<SaasDbContext>()
            .UseInMemoryDatabase(databaseName: $"SaasTestDb_{Guid.NewGuid()}")
            .Options;

        _db = new SaasDbContext(options);
        _sut = new SubscriptionService(_db);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private async Task<ServicePlan> SeedPlanAsync(string name = "Basic", decimal price = 299000, int days = 30, bool isActive = true)
    {
        var plan = new ServicePlan
        {
            Id = Guid.NewGuid(),
            PlanName = name,
            Price = price,
            DurationDays = days,
            IsActive = isActive,
            Features = "{}",
            CreatedAt = DateTime.UtcNow
        };
        _db.ServicePlans.Add(plan);
        await _db.SaveChangesAsync();
        return plan;
    }

    private async Task<Store> SeedStoreAsync()
    {
        var store = new Store
        {
            Id = Guid.NewGuid(),
            StoreName = "Test Store",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Stores.Add(store);
        await _db.SaveChangesAsync();
        return store;
    }

    // ============ GET ALL PLANS ============

    [Fact]
    public async Task GetAllPlansAsync_ReturnsOnlyActivePlans_ExcludingTrial()
    {
        // Arrange
        await SeedPlanAsync("Basic", 299000, 30, true);
        await SeedPlanAsync("Pro", 599000, 90, true);
        await SeedPlanAsync("Trial", 0, 7, true);       // Should be excluded
        await SeedPlanAsync("Inactive", 100000, 30, false); // Should be excluded

        // Act
        var plans = await _sut.GetAllPlansAsync();
        var planList = plans.ToList();

        // Assert
        Assert.Equal(2, planList.Count);
        Assert.DoesNotContain(planList, p => p.PlanName == "Trial");
        Assert.DoesNotContain(planList, p => p.PlanName == "Inactive");
    }

    [Fact]
    public async Task GetAllPlansAsync_OrderedByPrice()
    {
        // Arrange
        await SeedPlanAsync("Pro", 599000, 90);
        await SeedPlanAsync("Basic", 299000, 30);

        // Act
        var plans = (await _sut.GetAllPlansAsync()).ToList();

        // Assert
        Assert.True(plans[0].Price < plans[1].Price);
    }

    // ============ CREATE PENDING PAYMENT ============

    [Fact]
    public async Task CreatePendingPaymentAsync_ValidPlan_CreatesPaymentAndSubscription()
    {
        // Arrange
        var plan = await SeedPlanAsync("Basic", 299000, 30);
        var store = await SeedStoreAsync();
        var userId = Guid.NewGuid();

        // Act
        var (payment, returnedPlan) = await _sut.CreatePendingPaymentAsync(store.Id, plan.Id, userId);

        // Assert
        Assert.NotNull(payment);
        Assert.Equal("Pending", payment.Status);
        Assert.Equal(299000, payment.Amount);
        Assert.Equal(userId, payment.UserId);
        Assert.Equal("Basic", returnedPlan.PlanName);

        // Verify subscription was also created
        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.Id == payment.SubscriptionId);
        Assert.NotNull(subscription);
        Assert.Equal("Pending", subscription.Status);
        Assert.Equal(store.Id, subscription.StoreId);
    }

    [Fact]
    public async Task CreatePendingPaymentAsync_InvalidPlanId_ThrowsException()
    {
        // Arrange
        var store = await SeedStoreAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(
            () => _sut.CreatePendingPaymentAsync(store.Id, Guid.NewGuid(), Guid.NewGuid())
        );
        Assert.Equal("Service plan not found", ex.Message);
    }

    [Fact]
    public async Task CreatePendingPaymentAsync_InactivePlan_ThrowsException()
    {
        // Arrange
        var plan = await SeedPlanAsync("Deprecated", 100000, 30, false);
        var store = await SeedStoreAsync();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(
            () => _sut.CreatePendingPaymentAsync(store.Id, plan.Id, Guid.NewGuid())
        );
        Assert.Equal("This plan is not available", ex.Message);
    }

    // ============ ACTIVATE SUBSCRIPTION ============

    [Fact]
    public async Task ActivateSubscriptionAsync_ValidPayment_ActivatesSubscription()
    {
        // Arrange
        var plan = await SeedPlanAsync("Basic", 299000, 30);
        var store = await SeedStoreAsync();
        store.IsActive = false;
        await _db.SaveChangesAsync();

        var subscription = new _360Retail.Services.Saas.Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            PlanId = plan.Id,
            Status = "Pending",
            AutoRenew = false
        };
        _db.Subscriptions.Add(subscription);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            Amount = 299000,
            Status = "Pending",
            PaymentMethod = "VNPay",
            Provider = "VNPay",
            PaymentDate = DateTime.UtcNow,
            UserId = Guid.NewGuid()
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        // Act
        var (success, userId) = await _sut.ActivateSubscriptionAsync(payment.Id, "TXN_12345");

        // Assert
        Assert.True(success);
        Assert.NotNull(userId);

        // Verify payment updated
        var updatedPayment = await _db.Payments.FindAsync(payment.Id);
        Assert.Equal("Completed", updatedPayment?.Status);
        Assert.Equal("TXN_12345", updatedPayment?.TransactionCode);

        // Verify subscription activated
        var updatedSub = await _db.Subscriptions.FindAsync(subscription.Id);
        Assert.Equal("Active", updatedSub?.Status);
        Assert.NotNull(updatedSub?.StartDate);
        Assert.NotNull(updatedSub?.EndDate);
    }

    [Fact]
    public async Task ActivateSubscriptionAsync_PaymentNotFound_ReturnsFalse()
    {
        // Act
        var (success, userId) = await _sut.ActivateSubscriptionAsync(Guid.NewGuid(), "TXN");

        // Assert
        Assert.False(success);
        Assert.Null(userId);
    }

    // ============ MARK PAYMENT FAILED ============

    [Fact]
    public async Task MarkPaymentFailedAsync_ValidPayment_UpdatesStatus()
    {
        // Arrange
        var plan = await SeedPlanAsync();
        var store = await SeedStoreAsync();

        var subscription = new _360Retail.Services.Saas.Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            PlanId = plan.Id,
            Status = "Pending"
        };
        _db.Subscriptions.Add(subscription);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            Amount = 299000,
            Status = "Pending",
            PaymentMethod = "VNPay",
            PaymentDate = DateTime.UtcNow,
            UserId = Guid.NewGuid()
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.MarkPaymentFailedAsync(payment.Id, "Transaction declined");

        // Assert
        Assert.True(result);
        var updated = await _db.Payments.FindAsync(payment.Id);
        Assert.Equal("Failed", updated?.Status);
    }

    [Fact]
    public async Task MarkPaymentFailedAsync_NotFound_ReturnsFalse()
    {
        // Act
        var result = await _sut.MarkPaymentFailedAsync(Guid.NewGuid(), "Not found");

        // Assert
        Assert.False(result);
    }

    // ============ GET CURRENT SUBSCRIPTION ============

    [Fact]
    public async Task GetCurrentSubscriptionAsync_NoSubscription_ReturnsNoSubscriptionStatus()
    {
        // Arrange
        var store = await SeedStoreAsync();

        // Act
        var result = await _sut.GetCurrentSubscriptionAsync(store.Id);

        // Assert
        Assert.Equal("NoSubscription", result.Status);
        Assert.Null(result.SubscriptionId);
    }

    [Fact]
    public async Task GetCurrentSubscriptionAsync_ActiveSubscription_ReturnsDetails()
    {
        // Arrange
        var plan = await SeedPlanAsync("Pro", 599000, 90);
        var store = await SeedStoreAsync();

        _db.Subscriptions.Add(new _360Retail.Services.Saas.Domain.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            PlanId = plan.Id,
            Status = "Active",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(90)
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetCurrentSubscriptionAsync(store.Id);

        // Assert
        Assert.Equal("Active", result.Status);
        Assert.Equal("Pro", result.PlanName);
        Assert.True(result.DaysRemaining > 0);
    }
}
