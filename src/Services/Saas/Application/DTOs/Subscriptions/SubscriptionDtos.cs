namespace _360Retail.Services.Saas.Application.DTOs.Subscriptions;

// Request to purchase a plan - only planId needed
public record PurchasePlanRequest(Guid PlanId);

// Response with VNPay payment URL
public record PaymentUrlResponse(
    Guid PaymentId,
    string PaymentUrl,
    decimal Amount,
    string PlanName
);

// Service plan info for listing
public record ServicePlanDto(
    Guid Id,
    string PlanName,
    decimal Price,
    int DurationDays,
    string? Features,
    bool IsActive
);

// Current subscription status
public record SubscriptionStatusDto(
    Guid? SubscriptionId,
    string? PlanName,
    decimal? Price,
    DateTime? StartDate,
    DateTime? EndDate,
    string Status,
    int? DaysRemaining
);

// Payment result from VNPay callback
public record PaymentResultDto(
    bool Success,
    Guid PaymentId,
    string Message,
    string? RedirectUrl = null
);
