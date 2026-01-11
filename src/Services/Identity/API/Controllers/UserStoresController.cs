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
}
