using _360Retail.Services.Saas.Application.DTOs.Subscriptions;
using _360Retail.Services.Saas.Domain.Entities;

namespace _360Retail.Services.Saas.Application.Interfaces;

public interface ISubscriptionService
{
    Task<IEnumerable<ServicePlanDto>> GetAllPlansAsync();
    Task<SubscriptionStatusDto> GetCurrentSubscriptionAsync(Guid storeId);
    Task<(Payment payment, ServicePlan plan)> CreatePendingPaymentAsync(Guid storeId, Guid planId, Guid userId);
    Task<(bool success, Guid? userId)> ActivateSubscriptionAsync(Guid paymentId, string transactionCode);
    Task<bool> MarkPaymentFailedAsync(Guid paymentId, string reason);
}
