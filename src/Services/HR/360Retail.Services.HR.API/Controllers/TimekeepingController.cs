using _360Retail.Services.HR.Application.DTOs;
using _360Retail.Services.HR.Application.Interfaces;
using _360Retail.Services.HR.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace _360Retail.Services.HR.API.Controllers;

/// <summary>
/// Chấm công nhân viên (Check-in/out, upload selfie, lịch sử, tổng hợp)
/// </summary>
[ApiController]
[Route("api/timekeeping")]
[Authorize]
[RequiresActiveSubscription]
[_360Retail.Shared.Filters.RequiresFeature("has_gps_checkin")]
public class TimekeepingController : ControllerBase
{
    private readonly ITimekeepingService _timekeepingService;
    private readonly IStorageService _storageService;

    public TimekeepingController(ITimekeepingService timekeepingService, IStorageService storageService)
    {
        _timekeepingService = timekeepingService;
        _storageService = storageService;
    }

    #region Check-in / Check-out

    /// <summary>
    /// Nhân viên chấm công vào (check-in) — hỗ trợ GPS geofencing
    /// </summary>
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInDto dto)
    {
        var storeId = GetStoreId();
        if (storeId == null)
            return Unauthorized(new { success = false, message = "Không tìm thấy cửa hàng trong token" });

        var appUserId = GetAppUserId();
        if (appUserId == null)
            return Unauthorized(new { success = false, message = "Token không hợp lệ" });

        try
        {
            var result = await _timekeepingService.CheckInAsync(storeId.Value, appUserId.Value, dto);
            return Ok(new { success = true, message = "Chấm công vào thành công", data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Upload ảnh selfie lên Cloudinary, trả về URL để gửi kèm check-in
    /// </summary>
    [HttpPost("upload-selfie")]
    public async Task<IActionResult> UploadSelfie(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "Vui lòng chọn ảnh" });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { success = false, message = "Ảnh không được vượt quá 5MB" });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(new { success = false, message = "Chỉ chấp nhận ảnh JPEG, PNG hoặc WebP" });

        try
        {
            var imageUrl = await _storageService.SaveFileAsync(file, "timekeeping-selfies");
            return Ok(new { success = true, data = new { imageUrl }, message = "Upload ảnh thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = $"Upload thất bại: {ex.Message}" });
        }
    }

    /// <summary>
    /// Nhân viên chấm công ra (check-out)
    /// </summary>
    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutDto dto)
    {
        var storeId = GetStoreId();
        if (storeId == null)
            return Unauthorized(new { success = false, message = "Không tìm thấy cửa hàng trong token" });

        var appUserId = GetAppUserId();
        if (appUserId == null)
            return Unauthorized(new { success = false, message = "Token không hợp lệ" });

        try
        {
            var result = await _timekeepingService.CheckOutAsync(storeId.Value, appUserId.Value, dto);
            return Ok(new { success = true, message = "Chấm công ra thành công", data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Query

    /// <summary>
    /// Trạng thái chấm công hôm nay của nhân viên hiện tại
    /// </summary>
    [HttpGet("today")]
    public async Task<IActionResult> GetTodayStatus()
    {
        var storeId = GetStoreId();
        var appUserId = GetAppUserId();
        if (storeId == null || appUserId == null)
            return Unauthorized(new { success = false, message = "Token không hợp lệ" });

        var result = await _timekeepingService.GetTodayStatusAsync(storeId.Value, appUserId.Value);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Lịch sử chấm công (Manager/Owner: tất cả, Staff: của mình)
    /// Filter: employeeId, from, to
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHistory(
        [FromQuery] Guid? employeeId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var storeId = GetStoreId();
        var appUserId = GetAppUserId();
        if (storeId == null || appUserId == null)
            return Unauthorized(new { success = false, message = "Token không hợp lệ" });

        // Staff can only see their own records
        var roles = GetCurrentRoles();
        var isManagerOrOwner = roles.Any(r => r is "Manager" or "StoreOwner" or "Owner");

        if (!isManagerOrOwner)
        {
            // Force employeeId filter for non-managers (will be resolved in service)
            employeeId = null; // Will use appUserId lookup in service
        }

        var result = await _timekeepingService.GetHistoryAsync(
            storeId.Value, employeeId, from, to, page, pageSize);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Tổng hợp chấm công theo tháng (Manager/Owner only)
    /// </summary>
    [HttpGet("summary")]
    [Authorize(Roles = "Manager,StoreOwner,Owner")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] int? month,
        [FromQuery] int? year)
    {
        var storeId = GetStoreId();
        if (storeId == null)
            return Unauthorized(new { success = false, message = "Token không hợp lệ" });

        var now = DateTime.UtcNow;
        var m = month ?? now.Month;
        var y = year ?? now.Year;

        var result = await _timekeepingService.GetSummaryAsync(storeId.Value, m, y);
        return Ok(new { success = true, data = result });
    }

    #endregion

    #region Helpers

    private Guid? GetAppUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var id) ? id : null;
    }

    private Guid? GetStoreId()
    {
        var storeId = User.FindFirstValue("store_id");
        return Guid.TryParse(storeId, out var id) ? id : null;
    }

    private string[] GetCurrentRoles()
    {
        return User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
    }

    #endregion
}
