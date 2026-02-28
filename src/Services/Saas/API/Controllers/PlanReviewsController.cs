using _360Retail.Services.Saas.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace _360Retail.Services.Saas.API.Controllers;

/// <summary>
/// Đánh giá gói dịch vụ (Plan Reviews) — Store Owner đánh giá gói đã mua
/// </summary>
[ApiController]
[Route("api/plan-reviews")]
public class PlanReviewsController : ControllerBase
{
    private readonly IPlanReviewService _reviewService;

    public PlanReviewsController(IPlanReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>
    /// Tạo đánh giá cho gói đã mua (StoreOwner/Owner only, phải có subscription)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "StoreOwner,Owner")]
    public async Task<IActionResult> Create([FromBody] CreatePlanReviewDto dto)
    {
        var userId = GetUserId();
        var storeId = GetStoreId();
        if (userId == null || storeId == null)
            return Unauthorized(new { success = false, message = "Token không hợp lệ" });

        try
        {
            var result = await _reviewService.CreateAsync(userId.Value, storeId.Value, dto);
            return CreatedAtAction(null, new { success = true, message = "Đánh giá thành công", data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Xem đánh giá của mình cho 1 gói
    /// </summary>
    [HttpGet("me/{planId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetMyReview(Guid planId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { success = false, message = "Token không hợp lệ" });

        var review = await _reviewService.GetMyReviewAsync(userId.Value, planId);
        return Ok(new { success = true, data = review });
    }

    /// <summary>
    /// Xem tất cả đánh giá cho 1 gói (Public — không cần auth)
    /// </summary>
    [HttpGet("plan/{planId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByPlan(
        Guid planId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var reviews = await _reviewService.GetByPlanAsync(planId, page, pageSize);
        return Ok(new { success = true, data = reviews });
    }

    /// <summary>
    /// Tổng hợp đánh giá cho 1 gói (Public)
    /// </summary>
    [HttpGet("plan/{planId:guid}/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSummary(Guid planId)
    {
        var summary = await _reviewService.GetSummaryAsync(planId);
        return Ok(new { success = true, data = summary });
    }

    /// <summary>
    /// Tổng hợp đánh giá cho TẤT CẢ gói (Public — hiển thị trên trang giá)
    /// </summary>
    [HttpGet("summary")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllSummaries()
    {
        var summaries = await _reviewService.GetAllPlanSummariesAsync();
        return Ok(new { success = true, data = summaries });
    }

    // ===== SUPER ADMIN ENDPOINTS =====

    /// <summary>
    /// [SuperAdmin] Xem tất cả reviews toàn hệ thống (filter theo plan, rating)
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> AdminGetAll(
        [FromQuery] Guid? planId,
        [FromQuery] int? rating,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var reviews = await _reviewService.GetAllReviewsAsync(planId, rating, page, pageSize);
        return Ok(new { success = true, data = reviews });
    }

    /// <summary>
    /// [SuperAdmin] Dashboard thống kê reviews — phân loại theo từng gói
    /// </summary>
    [HttpGet("admin/dashboard")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> AdminDashboard()
    {
        var dashboard = await _reviewService.GetAdminDashboardAsync();
        return Ok(new { success = true, data = dashboard });
    }

    /// <summary>
    /// [SuperAdmin] Xóa review spam/vi phạm
    /// </summary>
    [HttpDelete("admin/{reviewId:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> AdminDelete(Guid reviewId)
    {
        var deleted = await _reviewService.DeleteAsync(reviewId);
        if (!deleted)
            return NotFound(new { success = false, message = "Review không tồn tại" });

        return Ok(new { success = true, message = "Đã xóa review thành công" });
    }

    #region Helpers

    private Guid? GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var id) ? id : null;
    }

    private Guid? GetStoreId()
    {
        var storeId = User.FindFirstValue("store_id");
        return Guid.TryParse(storeId, out var id) ? id : null;
    }

    #endregion
}
