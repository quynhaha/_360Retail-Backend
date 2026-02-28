using _360Retail.Services.Sales.Application.Interfaces;
using _360Retail.Services.Sales.Infrastructure.Services;
using _360Retail.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _360Retail.Services.Sales.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "StoreOwner,Manager")]
[RequiresActiveSubscription]
public class DashboardController : BaseApiController
{
    private readonly IDashboardService _dashboardService;
    private readonly CacheService _cache;

    public DashboardController(IDashboardService dashboardService, CacheService cache)
    {
        _dashboardService = dashboardService;
        _cache = cache;
    }

    /// <summary>
    /// Get dashboard overview: total revenue, orders, customers, products + growth comparison
    /// Cached for 5 minutes
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var storeId = GetCurrentStoreId();
        var (fromDate, toDate) = GetDateRange(from, to);
        var cacheKey = $"dashboard:overview:{storeId}:{fromDate:yyyyMMdd}:{toDate:yyyyMMdd}";
        var result = await _cache.GetOrSetAsync(cacheKey,
            () => _dashboardService.GetOverviewAsync(storeId, fromDate, toDate),
            TimeSpan.FromMinutes(5));
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Get revenue chart data grouped by day/week/month
    /// Cached for 5 minutes
    /// </summary>
    [HttpGet("revenue-chart")]
    public async Task<IActionResult> GetRevenueChart(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string groupBy = "day")
    {
        var storeId = GetCurrentStoreId();
        var (fromDate, toDate) = GetDateRange(from, to);
        var cacheKey = $"dashboard:revenue:{storeId}:{fromDate:yyyyMMdd}:{toDate:yyyyMMdd}:{groupBy}";
        var result = await _cache.GetOrSetAsync(cacheKey,
            () => _dashboardService.GetRevenueChartAsync(storeId, fromDate, toDate, groupBy),
            TimeSpan.FromMinutes(5));
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Get top selling products by revenue
    /// Cached for 10 minutes
    /// </summary>
    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopProducts(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int top = 10)
    {
        var storeId = GetCurrentStoreId();
        var (fromDate, toDate) = GetDateRange(from, to);
        var cacheKey = $"dashboard:top-products:{storeId}:{fromDate:yyyyMMdd}:{toDate:yyyyMMdd}:{top}";
        var result = await _cache.GetOrSetAsync(cacheKey,
            () => _dashboardService.GetTopProductsAsync(storeId, fromDate, toDate, top),
            TimeSpan.FromMinutes(10));
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Get order status distribution (for pie/donut chart) — NOT cached (changes frequently)
    /// </summary>
    [HttpGet("order-status")]
    public async Task<IActionResult> GetOrderStatus(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var storeId = GetCurrentStoreId();
        var (fromDate, toDate) = GetDateRange(from, to);
        var result = await _dashboardService.GetOrderStatusAsync(storeId, fromDate, toDate);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Get inventory summary: stock levels, low-stock alerts
    /// Cached for 3 minutes
    /// </summary>
    [HttpGet("inventory-summary")]
    public async Task<IActionResult> GetInventorySummary()
    {
        var storeId = GetCurrentStoreId();
        var cacheKey = $"dashboard:inventory:{storeId}";
        var result = await _cache.GetOrSetAsync(cacheKey,
            () => _dashboardService.GetInventorySummaryAsync(storeId),
            TimeSpan.FromMinutes(3));
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Get recent activity timeline (orders + inventory tickets) — NOT cached (real-time)
    /// </summary>
    [HttpGet("recent-activity")]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int limit = 20)
    {
        var storeId = GetCurrentStoreId();
        var result = await _dashboardService.GetRecentActivityAsync(storeId, limit);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Default date range: last 30 days
    /// </summary>
    private (DateTime from, DateTime to) GetDateRange(DateTime? from, DateTime? to)
    {
        var toDate = to ?? DateTime.UtcNow;
        var fromDate = from ?? toDate.AddDays(-30);
        return (fromDate, toDate);
    }
}
