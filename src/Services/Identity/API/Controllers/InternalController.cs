using _360Retail.Services.Identity.Application.DTOs;
using _360Retail.Services.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _360Retail.Services.Identity.API.Controllers;

/// <summary>
/// Internal APIs for cross-service communication
/// </summary>
[ApiController]
[Route("identity/internal")]
public class InternalController : ControllerBase
{
    private readonly IdentityDbContext _db;

    public InternalController(IdentityDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get user info by ID (for HR to fetch email, username, phone)
    /// </summary>
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == id);
        
        if (user == null)
            return NotFound(new { success = false, message = "User not found" });

        return Ok(new
        {
            userName = user.UserName,
            email = user.Email,
            phoneNumber = user.PhoneNumber
        });
    }

    /// <summary>
    /// Update user profile (partial update) - called by HR service
    /// </summary>
    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserProfileDto dto)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == id);
        
        if (user == null)
            return NotFound(new { success = false, message = "User not found" });

        // Partial update
        if (!string.IsNullOrWhiteSpace(dto.UserName))
            user.UserName = dto.UserName;

        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            user.PhoneNumber = dto.PhoneNumber;

        await _db.SaveChangesAsync();

        return Ok(new { success = true, message = "User updated successfully" });
    }
}
