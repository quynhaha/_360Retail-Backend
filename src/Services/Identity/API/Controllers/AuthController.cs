using _360Retail.Services.Identity.Application.DTOs;
using _360Retail.Services.Identity.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;


namespace _360Retail.Services.Identity.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // LOGIN
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }

        // REGISTER OWNER (PUBLIC)
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserDto dto)
        {
            await _authService.RegisterAsync(dto);
            return Ok(new { message = "Register successful" });
        }

        // INVITE STAFF (OWNER)
        [Authorize]
        [Authorize(Roles = "StoreOwner")]
        [HttpPost("invite-staff")]
        public async Task<IActionResult> InviteStaff(
            [FromQuery] Guid storeId,
            [FromBody] InviteStaffDto dto
        )
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized();

            var ownerUserId = Guid.Parse(userIdClaim);

            await _authService.InviteStaffAsync(ownerUserId, storeId, dto);
            return Ok(new { message = "Staff invited successfully" });
        }

        // ACTIVATE ACCOUNT (PUBLIC)
        [HttpPost("activate")]
        public async Task<IActionResult> ActivateAccount([FromBody] ActivateAccountDto dto)
        {
            await _authService.ActivateAccountAsync(dto);
            return Ok(new { message = "Account activated successfully" });
        }

        // ASSIGN STORE (INTERNAL/DEV)
        [Authorize]
        [HttpPost("assign-store")]
        public async Task<IActionResult> AssignStore([FromBody] AssignStoreDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized();

            await _authService.AssignStoreAsync(Guid.Parse(userIdClaim), dto);
            return Ok(new { message = "Store assigned successfully" });
        }

        // ACCESS REFRESH / SWITCH STORE (NEW)
        [Authorize]
        [HttpPost("refresh-access")]
        public async Task<IActionResult> RefreshAccess([FromQuery] Guid? storeId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized();

            var result = await _authService.RefreshAccessAsync(Guid.Parse(userIdClaim), storeId);
            return Ok(result);
        }

        // DEBUG / TEST JWT (SWAGGER)
        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(User.Claims.Select(c => new
            {
                c.Type,
                c.Value
            }));
        }
    }
}
