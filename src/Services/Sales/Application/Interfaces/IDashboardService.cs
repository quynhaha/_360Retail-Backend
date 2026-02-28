using _360Retail.Services.Sales.Application.DTOs;

namespace _360Retail.Services.Sales.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync(Guid storeId, DateTime from, DateTime to);
    Task<RevenueChartDto> GetRevenueChartAsync(Guid storeId, DateTime from, DateTime to, string groupBy);
    Task<List<TopProductDto>> GetTopProductsAsync(Guid storeId, DateTime from, DateTime to, int top);
    Task<OrderStatusSummaryDto> GetOrderStatusAsync(Guid storeId, DateTime from, DateTime to);
    Task<InventorySummaryDto> GetInventorySummaryAsync(Guid storeId);
    Task<RecentActivityDto> GetRecentActivityAsync(Guid storeId, int limit);
}
