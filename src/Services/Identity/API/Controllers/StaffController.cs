using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using _360Retail.Services.Identity.Application.DTOs;
using _360Retail.Services.Identity.Application.Interfaces;

namespace _360Retail.Services.Identity.API.Controllers;

[ApiController]
[Route("identity/staff")]
[Authorize(Roles = "Admin,StoreOwner")]
public class StaffController : ControllerBase
{
    private readonly IUserInvitationService _invitationService;

    public StaffController(IUserInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteStaff([FromBody] InviteUserDto dto)
    {
        await _invitationService.InviteUserAsync(dto);
        return Ok(new { message = "Invitation sent successfully" });
    }
}
