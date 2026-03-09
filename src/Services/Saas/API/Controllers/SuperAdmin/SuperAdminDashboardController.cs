using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _360Retail.Services.Saas.Application.Interfaces.SuperAdmin;
using _360Retail.Services.Saas.Infrastructure.Services.Caching;

namespace _360Retail.Services.Saas.API.Controllers.SuperAdmin;

[ApiController]
[Route("api/super-admin/saas/dashboard")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminDashboardController : ControllerBase
{
    private readonly ISuperAdminDashboardService _dashboardService;
    private readonly CacheService _cache;

    public SuperAdminDashboardController(ISuperAdminDashboardService dashboardService, CacheService cache)
    {
        _dashboardService = dashboardService;
        _cache = cache;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var cacheKey = "superadmin:dashboard:overview";
        // Cache for 10 minutes to reduce DB load
        var result = await _cache.GetOrSetAsync(cacheKey, 
            () => _dashboardService.GetOverviewAsync(), 
            TimeSpan.FromMinutes(10));
            
        return Ok(new { success = true, data = result });
    }

    [HttpGet("revenue-chart")]
    public async Task<IActionResult> GetRevenueChart(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string groupBy = "month")
    {
        var toDate = to ?? DateTime.UtcNow;
        var fromDate = from ?? toDate.AddMonths(-12);
        
        var cacheKey = $"superadmin:dashboard:revenue:{fromDate:yyyyMMdd}:{toDate:yyyyMMdd}:{groupBy}";
        // Cache for 10 minutes
        var result = await _cache.GetOrSetAsync(cacheKey,
            () => _dashboardService.GetRevenueChartAsync(fromDate, toDate, groupBy),
            TimeSpan.FromMinutes(10));
            
        return Ok(new { success = true, data = result });
    }

    [HttpGet("plan-distribution")]
    public async Task<IActionResult> GetPlanDistribution()
    {
        var cacheKey = "superadmin:dashboard:plan-distribution";
        // Cache for 10 minutes
        var result = await _cache.GetOrSetAsync(cacheKey,
            () => _dashboardService.GetPlanDistributionAsync(),
            TimeSpan.FromMinutes(10));
            
        return Ok(new { success = true, data = result });
    }
}
