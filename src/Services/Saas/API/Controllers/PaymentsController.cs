using Microsoft.AspNetCore.Mvc;
using _360Retail.Services.Saas.Application.DTOs.Subscriptions;
using _360Retail.Services.Saas.Application.Interfaces;
using _360Retail.Services.Saas.API.Services;
using _360Retail.Services.Saas.Infrastructure.HttpClients;
using Microsoft.AspNetCore.Authorization;

namespace _360Retail.Services.Saas.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly VNPayService _vnpayService;
    private readonly SePayService _sePayService;
    private readonly IIdentityClient _identityClient;
    private readonly IConfiguration _config;

    public PaymentsController(
        ISubscriptionService subscriptionService,
        VNPayService vnpayService,
        SePayService sePayService,
        IIdentityClient identityClient,
        IConfiguration config)
    {
        _subscriptionService = subscriptionService;
        _vnpayService = vnpayService;
        _sePayService = sePayService;
        _identityClient = identityClient;
        _config = config;
    }

    /// <summary>
    /// Initiate payment for an existing pending payment (e.g., new store subscription)
    /// Returns payment URL for client to redirect
    /// </summary>
    /// <summary>
    /// Initiate payment — supports ?provider=vnpay (default) or ?provider=sepay
    /// VNPay: returns redirect URL | SePay: returns QR code + bank info
    /// </summary>
    [HttpGet("initiate")]
    public async Task<IActionResult> InitiatePayment([FromQuery] Guid paymentId, [FromQuery] string provider = "vnpay")
    {
        var payment = await _subscriptionService.GetPaymentByIdAsync(paymentId);
        
        if (payment == null)
            return NotFound(new { success = false, message = "Không tìm thấy thanh toán" });

        if (payment.Status != "Pending")
            return BadRequest(new { success = false, message = "Thanh toán không ở trạng thái chờ xử lý" });

        // Get plan info
        var planInfo = await _subscriptionService.GetPaymentPlanInfoAsync(paymentId);

        // === SePay: return QR code + bank transfer info ===
        if (provider.Equals("sepay", StringComparison.OrdinalIgnoreCase))
        {
            var sePayResult = _sePayService.CreatePaymentInfo(
                payment.Id,
                payment.Amount,
                planInfo?.PlanName ?? "360Retail"
            );

            return Ok(new { success = true, data = sePayResult });
        }

        // === VNPay (default): return redirect URL ===
        var returnUrl = _config["VNPay:ReturnUrl"] ?? "http://localhost:5001/api/payments/vnpay-return";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var orderInfo = $"Thanh toan goi {planInfo?.PlanName ?? "360Retail"} - 360Retail";
        var paymentUrl = _vnpayService.CreatePaymentUrl(
            payment.Id,
            payment.Amount,
            orderInfo,
            returnUrl,
            ipAddress
        );

        return Ok(new
        {
            success = true,
            paymentId = payment.Id,
            paymentUrl = paymentUrl,
            amount = payment.Amount,
            planName = planInfo?.PlanName
        });
    }

    /// <summary>
    /// VNPay return URL - User is redirected here after payment
    /// </summary>
    [AllowAnonymous]  // VNPay callback — no JWT
    [HttpGet("vnpay-return")]
    public async Task<IActionResult> VNPayReturn()
    {
        var isValid = _vnpayService.ValidateCallback(
            Request.Query,
            out var transactionStatus,
            out var paymentId
        );

        if (!isValid)
        {
            var frontendUrlInvalid = _config["ServiceUrls:FrontendUrl"] ?? "http://localhost:3000";
            var invalidMessage = "Chữ ký từ VNPay không hợp lệ";
            var invalidRedirect = $"{frontendUrlInvalid}/payment/failed?paymentId={Guid.Empty}&message={Uri.EscapeDataString(invalidMessage)}";
            return Redirect(invalidRedirect);
        }

        var frontendUrl = _config["ServiceUrls:FrontendUrl"] ?? "http://localhost:3000";

        if (_vnpayService.IsPaymentSuccess(transactionStatus))
        {
            // Payment successful
            var transactionCode = Request.Query["vnp_TransactionNo"].ToString();
            var (activated, userId) = await _subscriptionService.ActivateSubscriptionAsync(paymentId, transactionCode);

            if (activated)
            {
                // Update user status in Identity service from Trial to Active
                if (userId.HasValue)
                {
                    await _identityClient.ActivateUserSubscriptionAsync(userId.Value);
                }

                var successRedirect = $"{frontendUrl}/payment/success?paymentId={paymentId}";
                return Redirect(successRedirect);
            }
            else
            {
                var notFoundMessage = "Không tìm thấy thông tin thanh toán";
                var notFoundRedirect = $"{frontendUrl}/payment/failed?paymentId={paymentId}&message={Uri.EscapeDataString(notFoundMessage)}";
                return Redirect(notFoundRedirect);
            }
        }
        else
        {
            // Payment failed
            var errorMessage = GetVNPayErrorMessage(transactionStatus);
            await _subscriptionService.MarkPaymentFailedAsync(paymentId, errorMessage);

            var failedRedirect = $"{frontendUrl}/payment/failed?paymentId={paymentId}&message={Uri.EscapeDataString(errorMessage)}";
            return Redirect(failedRedirect);
        }
    }

    /// <summary>
    /// Check payment status — FE polls this after showing QR code
    /// Returns: Pending, Completed, Failed, Expired
    /// </summary>
    [AllowAnonymous]  // FE polls by paymentId during payment flow
    [HttpGet("{paymentId}/status")]
    public async Task<IActionResult> GetPaymentStatus(Guid paymentId)
    {
        var payment = await _subscriptionService.GetPaymentByIdAsync(paymentId);

        if (payment == null)
            return NotFound(new { success = false, message = "Không tìm thấy thanh toán" });

        return Ok(new
        {
            success = true,
            paymentId = payment.Id,
            status = payment.Status,
            amount = payment.Amount,
            paymentDate = payment.PaymentDate,
            transactionCode = payment.TransactionCode
        });
    }

    private static string GetVNPayErrorMessage(string responseCode)
    {
        return responseCode switch
        {
            "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
            "09" => "Giao dịch không thành công do: Thẻ/Tài khoản chưa đăng ký dịch vụ InternetBanking.",
            "10" => "Giao dịch không thành công do: Xác thực thông tin thẻ/tài khoản không đúng quá 3 lần.",
            "11" => "Giao dịch không thành công do: Đã hết hạn chờ thanh toán.",
            "12" => "Giao dịch không thành công do: Thẻ/Tài khoản bị khóa.",
            "13" => "Giao dịch không thành công do: Mật khẩu xác thực giao dịch (OTP) không chính xác.",
            "24" => "Giao dịch không thành công do: Khách hàng hủy giao dịch.",
            "51" => "Giao dịch không thành công do: Tài khoản không đủ số dư.",
            "65" => "Giao dịch không thành công do: Tài khoản đã vượt quá hạn mức giao dịch trong ngày.",
            "75" => "Ngân hàng thanh toán đang bảo trì.",
            "79" => "Giao dịch không thành công do: Nhập sai mật khẩu thanh toán quá số lần quy định.",
            "99" => "Lỗi không xác định.",
            _ => $"Giao dịch thất bại với mã lỗi: {responseCode}"
        };
    }
}
