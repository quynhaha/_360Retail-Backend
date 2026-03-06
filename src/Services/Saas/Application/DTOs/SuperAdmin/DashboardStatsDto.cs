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
