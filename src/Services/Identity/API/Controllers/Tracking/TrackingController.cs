using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _360Retail.Services.Identity.Infrastructure.Services.Tracking;

namespace _360Retail.Services.Identity.API.Controllers.Tracking;

[ApiController]
[Route("api/tracking")]
public class TrackingController : ControllerBase
{
    private readonly RedisTrackingService _trackingService;

    public TrackingController(RedisTrackingService trackingService)
    {
        _trackingService = trackingService;
    }

    /// <summary>
    /// Track a landing page view
    /// </summary>
    [HttpPost("page-view")]
    [AllowAnonymous] // Public endpoint for landing page
    public async Task<IActionResult> TrackPageView()
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        await _trackingService.IncrementPageViewAsync(today);
        
        return Ok(new { success = true, date = today });
    }

    /// <summary>
    /// Internally get total page views for a specific date (Used by Super Admin Dashboard)
    /// </summary>
    [HttpGet("page-views/{date}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetPageViews(string date)
    {
        var count = await _trackingService.GetPageViewsAsync(date);
        return Ok(new { success = true, date = date, count = count });
    }
}
