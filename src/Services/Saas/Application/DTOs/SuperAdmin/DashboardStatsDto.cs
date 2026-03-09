namespace _360Retail.Services.Saas.Application.DTOs.SuperAdmin;

public class DashboardOverviewDto
{
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRecurringRevenue { get; set; }
    public int ActiveStores { get; set; }
    public int TrialStores { get; set; }
    public int ExpiredStores { get; set; }
    public decimal TrialToPaidConversionRate { get; set; }
}

public class RevenueStatDto
{
    public string Date { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class PlanDistributionDto
{
    public string PlanName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class AdminStoreDetailDto
{
    public Guid Id { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? OwnerEmail { get; set; }
    public string? CurrentPlan { get; set; }
    public string? SubscriptionStatus { get; set; }
    public DateTime? SubscriptionEndDate { get; set; }
}

public class AdminSubscriptionDto
{
    public Guid Id { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public decimal PlanPrice { get; set; }
    public string? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class AdminPaymentDto
{
    public Guid Id { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Status { get; set; }
    public string? Provider { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? TransactionCode { get; set; }
}
