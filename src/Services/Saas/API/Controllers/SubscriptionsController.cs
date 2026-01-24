using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using _360Retail.Services.Saas.Application.DTOs.Subscriptions;
using _360Retail.Services.Saas.Application.Interfaces;
using _360Retail.Services.Saas.API.Services;

namespace _360Retail.Services.Saas.API.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly VNPayService _vnpayService;
    private readonly IConfiguration _config;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        VNPayService vnpayService,
        IConfiguration config)
    {
        _subscriptionService = subscriptionService;
        _vnpayService = vnpayService;
        _config = config;
    }

    /// <summary>
    /// Get all available service plans
    /// </summary>
    [AllowAnonymous]
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await _subscriptionService.GetAllPlansAsync();
        return Ok(new { success = true, data = plans });
    }

    /// <summary>
    /// Get current store's subscription status
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMySubscription()
    {
        var storeId = GetStoreId();
        if (storeId == null)
            return Unauthorized(new { success = false, message = "No store assigned" });

        var status = await _subscriptionService.GetCurrentSubscriptionAsync(storeId.Value);
        return Ok(new { success = true, data = status });
    }

    /// <summary>
    /// Internal API: Get subscription status by storeId (called by Identity service)
    /// </summary>
    [AllowAnonymous]  // Internal API - should be protected by API key in production
    [HttpGet("store/{storeId:guid}/status")]
    public async Task<IActionResult> GetStoreSubscriptionStatus(Guid storeId)
    {
        var status = await _subscriptionService.GetCurrentSubscriptionAsync(storeId);
        return Ok(status);
    }

    /// <summary>
    /// Purchase a plan - returns VNPay payment URL
    /// </summary>
    [HttpPost("purchase")]
    public async Task<IActionResult> Purchase([FromBody] PurchasePlanRequest request)
    {
        var storeId = GetStoreId();
        if (storeId == null)
            return Unauthorized(new { success = false, message = "No store assigned" });

        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { success = false, message = "Invalid token" });

        try
        {
            var (payment, plan) = await _subscriptionService.CreatePendingPaymentAsync(storeId.Value, request.PlanId, userId.Value);

            // Build return URL from VNPay config
            var returnUrl = _config["VNPay:ReturnUrl"] ?? "http://localhost:5001/api/payments/vnpay-return";

            // Get client IP
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            // Create VNPay payment URL
            var orderInfo = $"Thanh toan goi {plan.PlanName} - 360Retail";
            var paymentUrl = _vnpayService.CreatePaymentUrl(
                payment.Id,
                payment.Amount,
                orderInfo,
                returnUrl,
                ipAddress
            );

            return Ok(new PaymentUrlResponse(
                payment.Id,
                paymentUrl,
                payment.Amount,
                plan.PlanName
            ));
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    private Guid? GetStoreId()
    {
        var storeId = User.FindFirstValue("store_id");
        return Guid.TryParse(storeId, out var id) ? id : null;
    }

    private Guid? GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var id) ? id : null;
    }
}
