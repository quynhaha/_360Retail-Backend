using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using _360Retail.Services.Identity.Application.Interfaces;

namespace _360Retail.Services.Identity.API.Controllers;

[ApiController]
[Route("api/subscription")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly IAuthService _authService;

    public SubscriptionController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Start 7-day trial for PotentialOwner. Creates trial store automatically.
    /// </summary>
    [HttpPost("start-trial")]
    public async Task<IActionResult> StartTrial([FromBody] StartTrialDto? dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();

        var userId = Guid.Parse(userIdClaim);
        var result = await _authService.StartTrialAsync(userId, dto?.StoreName);
        
        return Ok(result);
    }

    /// <summary>
    /// Get current subscription/trial status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();

        var userId = Guid.Parse(userIdClaim);
        var result = await _authService.GetSubscriptionStatusAsync(userId);
        
        return Ok(result);
    }
}

public record StartTrialDto(string? StoreName);
