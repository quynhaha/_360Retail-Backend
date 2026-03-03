using Microsoft.AspNetCore.Mvc;
using _360Retail.Services.Saas.Application.Interfaces;
using _360Retail.Services.Saas.API.Services;
using _360Retail.Services.Saas.Infrastructure.HttpClients;
using System.Text.Json;

namespace _360Retail.Services.Saas.API.Controllers;

/// <summary>
/// Webhook endpoint for SePay IPN (Instant Payment Notification)
/// SePay calls this when a bank transfer matching our payment code is detected
/// </summary>
[ApiController]
[Route("api/payments")]
public class SePayWebhookController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly SePayService _sePayService;
    private readonly IIdentityClient _identityClient;
    private readonly IConfiguration _config;
    private readonly ILogger<SePayWebhookController> _logger;

    public SePayWebhookController(
        ISubscriptionService subscriptionService,
        SePayService sePayService,
        IIdentityClient identityClient,
        IConfiguration config,
        ILogger<SePayWebhookController> logger)
    {
        _subscriptionService = subscriptionService;
        _sePayService = sePayService;
        _identityClient = identityClient;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// SePay IPN webhook — called when bank transfer is detected
    /// Must always return 200 OK to acknowledge receipt
    /// </summary>
    [HttpPost("sepay-webhook")]
    public async Task<IActionResult> SePayWebhook([FromBody] JsonElement body)
    {
        _logger.LogInformation("=== SePay Webhook Received ===");
        _logger.LogInformation("Body: {Body}", body.ToString());

        try
        {
            // Parse webhook data
            var id = body.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0;
            var transferType = body.TryGetProperty("transferType", out var ttProp) ? ttProp.GetString() : "";
            var transferAmount = body.TryGetProperty("transferAmount", out var amountProp) ? amountProp.GetDecimal() : 0;
            var content = body.TryGetProperty("content", out var contentProp) ? contentProp.GetString() : "";
            var code = body.TryGetProperty("code", out var codeProp) ? codeProp.GetString() : "";
            var referenceCode = body.TryGetProperty("referenceCode", out var refProp) ? refProp.GetString() : "";

            _logger.LogInformation(
                "SePay: id={Id}, type={Type}, amount={Amount}, code={Code}, content={Content}",
                id, transferType, transferAmount, code, content);

            // Only process incoming transfers
            if (transferType != "in")
            {
                _logger.LogInformation("SePay: Ignoring non-incoming transfer");
                return Ok(new { success = true, message = "Ignored (not incoming)" });
            }

            // Extract payment code from content or code field
            var paymentCode = !string.IsNullOrEmpty(code) ? code.ToUpper() :
                              _sePayService.ExtractPaymentCode(content);

            if (string.IsNullOrEmpty(paymentCode) || !paymentCode.StartsWith("360R"))
            {
                _logger.LogWarning("SePay: No valid payment code found in webhook");
                return Ok(new { success = true, message = "No matching payment code" });
            }

            _logger.LogInformation("SePay: Found payment code {Code}", paymentCode);

            // Find matching pending payment
            var pendingPayments = await _subscriptionService.GetPendingPaymentIdsAsync();
            var paymentId = _sePayService.ParsePaymentCodeToGuid(paymentCode, pendingPayments);

            if (paymentId == null)
            {
                _logger.LogWarning("SePay: Payment code {Code} does not match any pending payment", paymentCode);
                return Ok(new { success = true, message = "Payment code not matched" });
            }

            // Verify amount matches
            var payment = await _subscriptionService.GetPaymentByIdAsync(paymentId.Value);
            if (payment == null)
            {
                _logger.LogWarning("SePay: Payment {Id} not found", paymentId);
                return Ok(new { success = true, message = "Payment not found" });
            }

            if (transferAmount < payment.Amount)
            {
                _logger.LogWarning("SePay: Amount mismatch. Expected={Expected}, Received={Received}",
                    payment.Amount, transferAmount);
                return Ok(new { success = true, message = "Amount insufficient" });
            }

            // Activate subscription
            var transactionCode = $"SEPAY-{referenceCode}";
            var (activated, userId) = await _subscriptionService.ActivateSubscriptionAsync(paymentId.Value, transactionCode);

            if (activated)
            {
                _logger.LogInformation("SePay: Subscription activated for payment {Id}", paymentId);

                // Update user status from Trial to Active
                if (userId.HasValue)
                {
                    await _identityClient.ActivateUserSubscriptionAsync(userId.Value);
                }

                return Ok(new { success = true, message = "Payment processed and subscription activated" });
            }
            else
            {
                _logger.LogWarning("SePay: Failed to activate subscription for payment {Id}", paymentId);
                return Ok(new { success = true, message = "Activation failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SePay webhook processing error");
            // Always return 200 OK to prevent SePay from retrying
            return Ok(new { success = false, message = "Internal error" });
        }
    }

    /// <summary>
    /// SePay return URL — User is redirected here from bank app (optional)
    /// </summary>
    [HttpGet("sepay-return")]
    public IActionResult SePayReturn([FromQuery] string? paymentCode, [FromQuery] string? status)
    {
        var frontendUrl = _config["ServiceUrls:FrontendUrl"] ?? "http://localhost:3000";

        if (status == "success")
        {
            return Redirect($"{frontendUrl}/payment/processing?code={paymentCode}&provider=sepay");
        }

        return Redirect($"{frontendUrl}/payment/pending?code={paymentCode}&provider=sepay");
    }
}
