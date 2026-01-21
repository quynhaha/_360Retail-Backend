using _360Retail.Services.Saas.Application.DTOs.Subscriptions;
using _360Retail.Services.Saas.Application.Interfaces;
using _360Retail.Services.Saas.Domain.Entities;
using _360Retail.Services.Saas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace _360Retail.Services.Saas.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly SaasDbContext _db;

    public SubscriptionService(SaasDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ServicePlanDto>> GetAllPlansAsync()
    {
        var plans = await _db.ServicePlans
            .Where(p => p.IsActive == true && p.PlanName != "Trial")
            .OrderBy(p => p.Price)
            .ToListAsync();

        return plans.Select(p => new ServicePlanDto(
            p.Id,
            p.PlanName,
            p.Price,
            p.DurationDays,
            p.Features,
            p.IsActive ?? true
        ));
    }

    public async Task<SubscriptionStatusDto> GetCurrentSubscriptionAsync(Guid storeId)
    {
        var subscription = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.StoreId == storeId && s.Status == "Active")
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        if (subscription == null)
        {
            return new SubscriptionStatusDto(
                null, null, null, null, null,
                "NoSubscription", null
            );
        }

        var daysRemaining = subscription.EndDate.HasValue
            ? (int)(subscription.EndDate.Value - DateTime.UtcNow).TotalDays
            : 0;

        return new SubscriptionStatusDto(
            subscription.Id,
            subscription.Plan.PlanName,
            subscription.Plan.Price,
            subscription.StartDate,
            subscription.EndDate,
            subscription.Status ?? "Unknown",
            Math.Max(0, daysRemaining)
        );
    }

    public async Task<(Payment payment, ServicePlan plan)> CreatePendingPaymentAsync(Guid storeId, Guid planId, Guid userId)
    {
        var plan = await _db.ServicePlans.FindAsync(planId);
        if (plan == null)
            throw new Exception("Service plan not found");

        if (plan.IsActive != true)
            throw new Exception("This plan is not available");

        // Create a pending subscription
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            PlanId = planId,
            Status = "Pending",
            AutoRenew = false
        };

        _db.Subscriptions.Add(subscription);

        // Create pending payment with userId
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            Amount = plan.Price,
            PaymentMethod = "VNPay",
            Status = "Pending",
            Provider = "VNPay",
            PaymentDate = DateTime.UtcNow,
            UserId = userId
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return (payment, plan);
    }

    public async Task<(bool success, Guid? userId)> ActivateSubscriptionAsync(Guid paymentId, string transactionCode)
    {
        var payment = await _db.Payments
            .Include(p => p.Subscription)
            .ThenInclude(s => s.Plan)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
            return (false, null);

        // Update payment
        payment.Status = "Completed";
        payment.TransactionCode = transactionCode;
        payment.ProviderTransactionId = transactionCode;

        // Activate subscription
        var subscription = payment.Subscription;
        subscription.Status = "Active";
        subscription.StartDate = DateTime.UtcNow;
        subscription.EndDate = DateTime.UtcNow.AddDays(subscription.Plan.DurationDays);

        await _db.SaveChangesAsync();

        return (true, payment.UserId);
    }

    public async Task<bool> MarkPaymentFailedAsync(Guid paymentId, string reason)
    {
        var payment = await _db.Payments
            .Include(p => p.Subscription)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
            return false;

        payment.Status = "Failed";
        payment.ResponsePayload = reason;
        payment.Subscription.Status = "Failed";

        await _db.SaveChangesAsync();

        return true;
    }
}
