using Microsoft.AspNetCore.Mvc;
using _360Retail.Services.Saas.Application.DTOs.Subscriptions;
using _360Retail.Services.Saas.Application.Interfaces;
using _360Retail.Services.Saas.API.Services;
using _360Retail.Services.Saas.Infrastructure.HttpClients;

namespace _360Retail.Services.Saas.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly VNPayService _vnpayService;
    private readonly IIdentityClient _identityClient;
    private readonly IConfiguration _config;

    public PaymentsController(
        ISubscriptionService subscriptionService,
        VNPayService vnpayService,
        IIdentityClient identityClient,
        IConfiguration config)
    {
        _subscriptionService = subscriptionService;
        _vnpayService = vnpayService;
        _identityClient = identityClient;
        _config = config;
    }

    /// <summary>
    /// VNPay return URL - User is redirected here after payment
    /// </summary>
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
            return BadRequest(new PaymentResultDto(
                false,
                Guid.Empty,
                "Invalid signature from VNPay"
            ));
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
                
                return Ok(new PaymentResultDto(
                    true,
                    paymentId,
                    "Thanh toán thành công! Gói dịch vụ đã được kích hoạt.",
                    $"{frontendUrl}/payment/success?paymentId={paymentId}"
                ));
            }
            else
            {
                return BadRequest(new PaymentResultDto(
                    false,
                    paymentId,
                    "Không tìm thấy thông tin thanh toán"
                ));
            }
        }
        else
        {
            // Payment failed
            var errorMessage = GetVNPayErrorMessage(transactionStatus);
            await _subscriptionService.MarkPaymentFailedAsync(paymentId, errorMessage);

            return Ok(new PaymentResultDto(
                false,
                paymentId,
                errorMessage,
                $"{frontendUrl}/payment/failed?paymentId={paymentId}"
            ));
        }
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
