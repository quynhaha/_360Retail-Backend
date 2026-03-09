using _360Retail.Services.Saas.Application.DTOs.SuperAdmin;

namespace _360Retail.Services.Saas.Application.Interfaces.SuperAdmin;

public interface ISuperAdminDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync();
    Task<List<RevenueStatDto>> GetRevenueChartAsync(DateTime from, DateTime to, string groupBy);
    Task<List<PlanDistributionDto>> GetPlanDistributionAsync();
    Task<List<AdminStoreDetailDto>> GetAllStoresDetailAsync();
    Task<List<AdminSubscriptionDto>> GetAllSubscriptionsAsync(string? status, Guid? planId);
    Task<List<AdminPaymentDto>> GetAllPaymentsAsync(string? status, DateTime? from, DateTime? to);
}
