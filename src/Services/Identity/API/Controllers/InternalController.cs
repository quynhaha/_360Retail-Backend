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

    /// <summary>
    /// Update user's role in a store (called by HR service when Owner changes position)
    /// </summary>
    [HttpPut("users/{userId}/stores/{storeId}/role")]
    public async Task<IActionResult> UpdateUserRoleInStore(Guid userId, Guid storeId, [FromBody] UpdateRoleDto dto)
    {
        Console.WriteLine($"[DEBUG] UpdateUserRoleInStore called: userId={userId}, storeId={storeId}, dto.RoleInStore={dto?.RoleInStore ?? "NULL"}");
        
        if (dto == null || string.IsNullOrEmpty(dto.RoleInStore))
        {
            Console.WriteLine("[DEBUG] DTO is null or RoleInStore is empty");
            return BadRequest(new { success = false, message = "RoleInStore is required" });
        }
        
        var access = await _db.UserStoreAccess
            .FirstOrDefaultAsync(a => a.UserId == userId && a.StoreId == storeId);
        
        Console.WriteLine($"[DEBUG] Found access record: {(access != null ? $"Yes, current role={access.RoleInStore}" : "No")}");
        
        if (access == null)
            return NotFound(new { success = false, message = "User store access not found" });

        // Validate role
        var validRoles = new[] { "Staff", "Manager", "Owner" };
        if (!validRoles.Contains(dto.RoleInStore))
        {
            Console.WriteLine($"[DEBUG] Invalid role: {dto.RoleInStore}");
            return BadRequest(new { success = false, message = "Invalid role. Must be: Staff, Manager, or Owner" });
        }

        Console.WriteLine($"[DEBUG] Updating role from {access.RoleInStore} to {dto.RoleInStore}");
        access.RoleInStore = dto.RoleInStore;
        await _db.SaveChangesAsync();
        Console.WriteLine($"[DEBUG] Role updated successfully!");

        return Ok(new { success = true, message = $"Role updated to {dto.RoleInStore}" });
    }
}

public class UpdateRoleDto
{
    [System.Text.Json.Serialization.JsonPropertyName("roleInStore")]
    public string RoleInStore { get; set; } = null!;
}

