using _360Retail.Services.Saas.Infrastructure.Persistence;
using _360Retail.Shared.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _360Retail.Services.Saas.API.Controllers;

/// <summary>
/// Kiểm tra và thông báo subscription sắp hết hạn
/// </summary>
[ApiController]
[Route("api/subscriptions")]
public class SubscriptionNotificationsController : ControllerBase
{
    private readonly SaasDbContext _db;
    private readonly IEmailSender _emailSender;

    public SubscriptionNotificationsController(SaasDbContext db, IEmailSender emailSender)
    {
        _db = db;
        _emailSender = emailSender;
    }

    /// <summary>
    /// Kiểm tra tất cả subscription sắp hết hạn (trong N ngày tới) và gửi email cảnh báo.
    /// Dùng cho admin hoặc cron job gọi định kỳ.
    /// </summary>
    [HttpPost("check-expiry")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CheckExpiringSubscriptions([FromQuery] int daysAhead = 3)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

        // Find active subscriptions expiring within N days
        var expiringSubscriptions = await _db.Subscriptions
            .Include(s => s.Plan)
            .Include(s => s.Store)
            .Where(s => s.Status == "Active"
                && s.EndDate.HasValue
                && s.EndDate.Value <= cutoffDate
                && s.EndDate.Value > DateTime.UtcNow) // Not yet expired
            .ToListAsync();

        if (!expiringSubscriptions.Any())
            return Ok(new { message = "Không có subscription nào sắp hết hạn", count = 0 });

        var notified = new List<object>();

        foreach (var sub in expiringSubscriptions)
        {
            // Find store owner email from identity schema
            var ownerEmail = await _db.Database
                .SqlQueryRaw<string>(
                    @"SELECT au.email AS ""Value"" 
                      FROM identity.user_store_access usa 
                      JOIN identity.app_users au ON au.id = usa.user_id 
                      WHERE usa.store_id = {0} AND usa.role_in_store = 'Owner' 
                      LIMIT 1", sub.StoreId)
                .FirstOrDefaultAsync();

            var ownerName = await _db.Database
                .SqlQueryRaw<string>(
                    @"SELECT au.user_name AS ""Value"" 
                      FROM identity.user_store_access usa 
                      JOIN identity.app_users au ON au.id = usa.user_id 
                      WHERE usa.store_id = {0} AND usa.role_in_store = 'Owner' 
                      LIMIT 1", sub.StoreId)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(ownerEmail)) continue;

            var daysRemaining = (int)(sub.EndDate!.Value - DateTime.UtcNow).TotalDays;

            var html = EmailTemplateService.SubscriptionExpiry(
                ownerName ?? ownerEmail,
                sub.Store?.StoreName ?? "Cửa hàng",
                sub.Plan?.PlanName ?? "Gói dịch vụ",
                sub.EndDate!.Value,
                Math.Max(0, daysRemaining)
            );

            await _emailSender.SendAsync(
                ownerEmail,
                $"[360Retail] ⏰ Gói {sub.Plan?.PlanName} sắp hết hạn",
                html
            );

            notified.Add(new
            {
                storeId = sub.StoreId,
                storeName = sub.Store?.StoreName,
                planName = sub.Plan?.PlanName,
                endDate = sub.EndDate,
                daysRemaining,
                ownerEmail
            });
        }

        return Ok(new
        {
            message = $"Đã gửi thông báo cho {notified.Count} subscription sắp hết hạn",
            count = notified.Count,
            details = notified
        });
    }

    /// <summary>
    /// Kiểm tra subscription của store hiện tại (cho owner tự check)
    /// </summary>
    [HttpGet("my-expiry")]
    [Authorize(Roles = "StoreOwner")]
    public async Task<IActionResult> CheckMySubscriptionExpiry()
    {
        var storeId = User.FindFirst("store_id")?.Value;
        if (string.IsNullOrEmpty(storeId))
            return BadRequest(new { message = "Store context required" });

        var storeGuid = Guid.Parse(storeId);

        var subscription = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.StoreId == storeGuid && s.Status == "Active")
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        if (subscription == null)
            return Ok(new { message = "Không có subscription active", status = "NoSubscription" });

        var daysRemaining = subscription.EndDate.HasValue
            ? (int)(subscription.EndDate.Value - DateTime.UtcNow).TotalDays
            : 0;

        return Ok(new
        {
            planName = subscription.Plan?.PlanName,
            endDate = subscription.EndDate,
            daysRemaining = Math.Max(0, daysRemaining),
            isExpiringSoon = daysRemaining <= 3,
            status = daysRemaining <= 0 ? "Expired" : daysRemaining <= 3 ? "ExpiringSoon" : "Active"
        });
    }
}
