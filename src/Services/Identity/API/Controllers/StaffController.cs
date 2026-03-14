using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using _360Retail.Services.Identity.Application.DTOs;
using _360Retail.Services.Identity.Application.Interfaces;
using _360Retail.Shared.Common.Exceptions;

namespace _360Retail.Services.Identity.API.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize(Roles = "SuperAdmin,StoreOwner")]
public class StaffController : ControllerBase
{
    private readonly IUserInvitationService _invitationService;
    private readonly ILogger<StaffController> _logger;

    public StaffController(
        IUserInvitationService invitationService,
        ILogger<StaffController> logger)
    {
        _invitationService = invitationService;
        _logger = logger;
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteStaff([FromBody] InviteUserDto dto)
    {
        _logger.LogInformation("Invite staff request: Email={Email}, Role={Role}, StoreId={StoreId}",
            dto.Email, dto.Role, dto.StoreId);

        await _invitationService.InviteUserAsync(dto);
        return Ok(new { message = "Invitation sent successfully" });
    }
}
