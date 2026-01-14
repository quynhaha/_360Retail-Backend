using _360Retail.Services.Identity.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace _360Retail.Services.Identity.API.Controllers;

[ApiController]
[Route("api/identity")]
[Authorize]
public class UserStoresController : ControllerBase
{
    private readonly IUserStoreAccessService _userStoreAccessService;

    public UserStoresController(IUserStoreAccessService userStoreAccessService)
    {
        _userStoreAccessService = userStoreAccessService;
    }

    [HttpGet("stores-my")]
    public async Task<IActionResult> GetMyStores()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var stores = await _userStoreAccessService.GetMyStoresAsync(Guid.Parse(userId));
        return Ok(stores);
    }

    [Authorize]
    [HttpGet("has-store-access")]
    public async Task<IActionResult> HasStoreAccess(
    Guid storeId,
    string role)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized();

        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var hasAccess = await _userStoreAccessService
            .HasStoreAccessAsync(userId, storeId, role);

        return Ok(hasAccess);
    }
}
