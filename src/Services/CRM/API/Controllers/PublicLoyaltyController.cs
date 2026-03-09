using Microsoft.AspNetCore.Mvc;
using _360Retail.Services.CRM.Application.Services;

namespace _360Retail.Services.CRM.API.Controllers;

/// <summary>
/// Public endpoint: Khách hàng tra điểm loyalty bằng SĐT (không cần login)
/// </summary>
[ApiController]
[Route("api/loyalty")]
public class PublicLoyaltyController : ControllerBase
{
    private readonly ILoyaltyService _loyaltyService;

    public PublicLoyaltyController(ILoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService;
    }

    /// <summary>
    /// Tra cứu điểm loyalty bằng SĐT
    /// Khách scan QR → nhập SĐT → xem điểm, rank
    /// </summary>
    [HttpGet("check")]
    public async Task<IActionResult> CheckByPhone(
        [FromQuery] string phone,
        [FromQuery] Guid storeId)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest(new { success = false, message = "Vui lòng nhập số điện thoại" });

        if (storeId == Guid.Empty)
            return BadRequest(new { success = false, message = "StoreId không hợp lệ" });

        var result = await _loyaltyService.GetCustomerByPhoneAsync(storeId, phone);

        if (result == null)
            return NotFound(new { success = false, message = "Không tìm thấy khách hàng với SĐT này" });

        return Ok(new { success = true, data = result });
    }
}
