namespace _360Retail.Services.Identity.Application.DTOs;

/// <summary>
/// Result of starting a trial period
/// </summary>
public record StartTrialResultDto(
    Guid StoreId,
    string StoreName,
    DateTime TrialEndDate,
    int DaysRemaining
);

/// <summary>
/// Current subscription/trial status
/// </summary>
public record SubscriptionStatusDto(
    string Status,              // "Registered", "Trial", "Active", "Expired"
    bool HasStore,
    Guid? StoreId,
    DateTime? TrialStartDate,
    DateTime? TrialEndDate,
    int? DaysRemaining,
    string? PlanName
);
