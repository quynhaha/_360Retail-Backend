using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _360Retail.Services.CRM.Application.DTOs;
using _360Retail.Services.CRM.Application.Services;
using _360Retail.Shared.Filters;
using System.Security.Claims;

namespace _360Retail.Services.CRM.API.Controllers;

/// <summary>
/// Quản lý đánh giá/feedback của khách hàng
/// </summary>
[ApiController]
[Route("api/feedback")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;

    public FeedbackController(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    /// <summary>
    /// [PUBLIC] Khách hàng feedback qua QR Code trên hóa đơn (không cần đăng nhập).
    /// URL trên QR: /api/feedback/public/{orderId}
    /// </summary>
    [HttpPost("public/{orderId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> CreatePublic(Guid orderId, [FromBody] CreatePublicFeedbackDto dto)
    {
        try
        {
            var result = await _feedbackService.CreatePublicAsync(orderId, dto);
            return CreatedAtAction(null, new { success = true, message = "Cảm ơn bạn đã đánh giá!", data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// [STAFF] Tạo feedback cho khách hàng (nhân viên nhập hộ)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "StoreOwner,Manager,Staff")]
    [RequiresFeature("has_feedback_qr")]
    public async Task<IActionResult> Create([FromBody] CreateFeedbackDto dto)
    {
        var storeId = GetStoreIdFromToken();
        if (storeId == Guid.Empty)
            return Unauthorized(new { success = false, message = "Không tìm thấy cửa hàng trong token" });

        var appUserId = GetAppUserId();

        try
        {
            var result = await _feedbackService.CreateAsync(storeId, dto, null);
            return CreatedAtAction(null, new { success = true, message = "Tạo feedback thành công", data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Danh sách feedback theo store (filter: rating, from, to)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "StoreOwner,Manager,Staff")]
    public async Task<IActionResult> GetByStore(
        [FromQuery] int? rating,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var storeId = GetStoreIdFromToken();
        if (storeId == Guid.Empty)
            return Unauthorized(new { success = false, message = "Không tìm thấy cửa hàng trong token" });

        if (rating.HasValue && (rating < 1 || rating > 5))
            return BadRequest(new { success = false, message = "Rating filter phải từ 1 đến 5" });

        var result = await _feedbackService.GetByStoreAsync(storeId, rating, from, to, page, pageSize);
        return Ok(new
        {
            success = true,
            data = result.Data,
            meta = new { page = result.Page, pageSize = result.PageSize, total = result.TotalCount }
        });
    }

    /// <summary>
    /// Tổng hợp rating (avg, count, distribution 1-5) — Manager/Owner
    /// </summary>
    [HttpGet("summary")]
    [Authorize(Roles = "StoreOwner,Manager")]
    public async Task<IActionResult> GetSummary()
    {
        var storeId = GetStoreIdFromToken();
        if (storeId == Guid.Empty)
            return Unauthorized(new { success = false, message = "Không tìm thấy cửa hàng trong token" });

        var result = await _feedbackService.GetSummaryAsync(storeId);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Feedback của một khách hàng cụ thể
    /// </summary>
    [HttpGet("~/api/customers/{customerId:guid}/feedback")]
    [Authorize(Roles = "StoreOwner,Manager,Staff")]
    public async Task<IActionResult> GetByCustomer(
        Guid customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var storeId = GetStoreIdFromToken();
        if (storeId == Guid.Empty)
            return Unauthorized(new { success = false, message = "Không tìm thấy cửa hàng trong token" });

        var result = await _feedbackService.GetByCustomerAsync(customerId, storeId, page, pageSize);
        return Ok(new
        {
            success = true,
            data = result.Data,
            meta = new { page = result.Page, pageSize = result.PageSize, total = result.TotalCount }
        });
    }

    #region Helpers

    private Guid GetStoreIdFromToken()
    {
        var storeIdClaim = User.FindFirst("store_id")?.Value;
        return storeIdClaim != null ? Guid.Parse(storeIdClaim) : Guid.Empty;
    }

    private Guid? GetAppUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var id) ? id : null;
    }

    #endregion
}
