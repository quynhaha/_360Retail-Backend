using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _360Retail.Services.Saas.Application.Interfaces.SuperAdmin;
using _360Retail.Services.Saas.Infrastructure.Persistence;
using _360Retail.Services.Saas.Infrastructure.Services.Caching;

namespace _360Retail.Services.Saas.API.Controllers.SuperAdmin;

[ApiController]
[Route("api/super-admin/saas/dashboard")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminDashboardController : ControllerBase
{
    private readonly ISuperAdminDashboardService _dashboardService;
    private readonly CacheService _cache;
    private readonly SaasDbContext _db;

    public SuperAdminDashboardController(ISuperAdminDashboardService dashboardService, CacheService cache, SaasDbContext db)
    {
        _dashboardService = dashboardService;
        _cache = cache;
        _db = db;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var cacheKey = "superadmin:dashboard:overview";
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
        var result = await _cache.GetOrSetAsync(cacheKey,
            () => _dashboardService.GetRevenueChartAsync(fromDate, toDate, groupBy),
            TimeSpan.FromMinutes(10));
            
        return Ok(new { success = true, data = result });
    }

    [HttpGet("plan-distribution")]
    public async Task<IActionResult> GetPlanDistribution()
    {
        var cacheKey = "superadmin:dashboard:plan-distribution";
        var result = await _cache.GetOrSetAsync(cacheKey,
            () => _dashboardService.GetPlanDistributionAsync(),
            TimeSpan.FromMinutes(10));
            
        return Ok(new { success = true, data = result });
    }

    [HttpGet("stores")]
    public async Task<IActionResult> GetAllStoresDetail()
    {
        var cacheKey = "superadmin:dashboard:stores";
        var result = await _cache.GetOrSetAsync(cacheKey,
            () => _dashboardService.GetAllStoresDetailAsync(),
            TimeSpan.FromMinutes(5));

        return Ok(new { success = true, data = result });
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetAllSubscriptions(
        [FromQuery] string? status,
        [FromQuery] Guid? planId)
    {
        var result = await _dashboardService.GetAllSubscriptionsAsync(status, planId);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetAllPayments(
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await _dashboardService.GetAllPaymentsAsync(status, from, to);
        return Ok(new { success = true, data = result });
    }

    // --- Subscription Management ---

    /// <summary>
    /// Admin hủy subscription
    /// </summary>
    [HttpPut("subscriptions/{id:guid}/cancel")]
    public async Task<IActionResult> CancelSubscription(Guid id)
    {
        var sub = await _db.Subscriptions
            .Include(s => s.Store)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sub == null)
            return NotFound(new { success = false, message = "Subscription not found" });

        if (sub.Status == "Cancelled")
            return BadRequest(new { success = false, message = "Subscription đã bị hủy trước đó" });

        sub.Status = "Cancelled";
        await _db.SaveChangesAsync();

        return Ok(new { success = true, message = $"Đã hủy subscription cho store '{sub.Store?.StoreName}'" });
    }

    /// <summary>
    /// Admin gia hạn subscription thêm N ngày
    /// </summary>
    [HttpPut("subscriptions/{id:guid}/extend")]
    public async Task<IActionResult> ExtendSubscription(Guid id, [FromBody] ExtendSubscriptionRequest request)
    {
        var sub = await _db.Subscriptions
            .Include(s => s.Store)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sub == null)
            return NotFound(new { success = false, message = "Subscription not found" });

        var oldEnd = sub.EndDate;
        sub.EndDate = (sub.EndDate ?? DateTime.UtcNow).AddDays(request.Days);
        
        if (sub.Status == "Expired" || sub.Status == "Cancelled")
            sub.Status = "Active";

        await _db.SaveChangesAsync();

        return Ok(new { success = true, message = $"Đã gia hạn thêm {request.Days} ngày cho store '{sub.Store?.StoreName}'", 
            data = new { oldEndDate = oldEnd, newEndDate = sub.EndDate } });
    }
}

public class ExtendSubscriptionRequest
{
    public int Days { get; set; } = 30;
}
