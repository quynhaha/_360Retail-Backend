using _360Retail.Services.Identity.Application.DTOs;
using _360Retail.Services.Identity.Application.Interfaces;
using _360Retail.Services.Identity.Infrastructure.Services;
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
        private readonly TokenBlacklistService _tokenBlacklist;

        public AuthController(IAuthService authService, TokenBlacklistService tokenBlacklist)
        {
            _authService = authService;
            _tokenBlacklist = tokenBlacklist;
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

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            await _authService.ChangePasswordAsync(Guid.Parse(userId), dto);

            return Ok(new
            {
                message = "Password changed successfully. Please login again."
            });
        }

        // EXTERNAL OAUTH LOGIN (Google, Facebook)
        /// <summary>
        /// Login with external OAuth provider (Google, Facebook)
        /// </summary>
        /// <remarks>
        /// Frontend should use Google Sign-In SDK to get the ID token,
        /// then send it here for backend validation and JWT generation.
        /// 
        /// Example request:
        /// ```json
        /// {
        ///     "provider": "Google",
        ///     "idToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ..."
        /// }
        /// ```
        /// </remarks>
        [HttpPost("external")]
        public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginDto dto)
        {
            var result = await _authService.ExternalLoginAsync(dto);
            return Ok(result);
        }

        // FORGOT PASSWORD (PUBLIC)
        /// <summary>
        /// Gửi mã reset password về email
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            await _authService.ForgotPasswordAsync(dto.Email);
            return Ok(new { message = "Nếu email tồn tại, mã xác nhận đã được gửi" });
        }

        // RESET PASSWORD (PUBLIC)
        /// <summary>
        /// Đặt lại mật khẩu bằng mã xác nhận
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            await _authService.ResetPasswordAsync(dto.Email, dto.ResetCode, dto.NewPassword);
            return Ok(new { message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại" });
        }

        /// <summary>
        /// Logout — blacklist the current JWT token in Redis
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var token = HttpContext.Request.Headers["Authorization"]
                .ToString().Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
                return BadRequest(new { message = "No token provided" });

            // Parse token to get expiry
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var remaining = jwt.ValidTo - DateTime.UtcNow;

            if (remaining > TimeSpan.Zero)
                await _tokenBlacklist.BlacklistAsync(token, remaining);

            return Ok(new { message = "Logged out successfully" });
        }

    }
}
