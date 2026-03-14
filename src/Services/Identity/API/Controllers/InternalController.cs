using _360Retail.Services.Identity.Application.DTOs;
using _360Retail.Services.Identity.Domain.Entities;
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
    private readonly ILogger<InternalController> _logger;

    public InternalController(IdentityDbContext db, ILogger<InternalController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Get user info by ID (for HR to fetch email, username, phone)
    /// </summary>
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == id);
        
        if (user == null)
            return NotFound(new { success = false, message = "Không tìm thấy người dùng" });

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
            return NotFound(new { success = false, message = "Không tìm thấy người dùng" });

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
    /// Also syncs the system role in user_roles table
    /// </summary>
    [HttpPut("users/{userId}/stores/{storeId}/role")]
    public async Task<IActionResult> UpdateUserRoleInStore(Guid userId, Guid storeId, [FromBody] UpdateRoleDto dto)
    {
        _logger.LogDebug("UpdateUserRoleInStore called: userId={UserId}, storeId={StoreId}, RoleInStore={RoleInStore}", userId, storeId, dto?.RoleInStore ?? "NULL");
        
        if (dto == null || string.IsNullOrEmpty(dto.RoleInStore))
        {
            _logger.LogDebug("DTO is null or RoleInStore is empty");
            return BadRequest(new { success = false, message = "Cần có vai trò trong cửa hàng" });
        }
        
        // 1. Update RoleInStore in user_store_access
        var access = await _db.UserStoreAccess
            .FirstOrDefaultAsync(a => a.UserId == userId && a.StoreId == storeId);
        
        _logger.LogDebug("Found access record: {Found}", access != null ? $"Yes, current role={access.RoleInStore}" : "No");
        
        if (access == null)
            return NotFound(new { success = false, message = "Không tìm thấy quyền truy cập cửa hàng" });

        // Validate role
        var validRoles = new[] { "Staff", "Manager", "Owner" };
        if (!validRoles.Contains(dto.RoleInStore))
        {
            _logger.LogDebug("Invalid role: {Role}", dto.RoleInStore);
            return BadRequest(new { success = false, message = "Vai trò không hợp lệ. Phải là: Staff, Manager hoặc Owner" });
        }

        _logger.LogDebug("Updating RoleInStore from {OldRole} to {NewRole}", access.RoleInStore, dto.RoleInStore);
        access.RoleInStore = dto.RoleInStore;

        // 2. Sync system role in user_roles table
        // Map RoleInStore to system role name
        var systemRoleName = dto.RoleInStore switch
        {
            "Owner" => "StoreOwner",
            "Manager" => "Manager",
            "Staff" => "Staff",
            _ => "Staff"
        };

        _logger.LogDebug("Syncing system role to: {SystemRoleName}", systemRoleName);

        // Get the user with their roles
        var user = await _db.AppUsers
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user != null)
        {
            // Get the target system role
            var targetRole = await _db.AppRoles
                .FirstOrDefaultAsync(r => r.RoleName == systemRoleName);

            if (targetRole != null)
            {
                // Remove old store-related roles (StoreOwner, Manager, Staff) - keep SuperAdmin/Customer if present
                var storeRoleNames = new[] { "StoreOwner", "Manager", "Staff" };
                var rolesToRemove = user.Roles.Where(r => storeRoleNames.Contains(r.RoleName)).ToList();
                
                foreach (var role in rolesToRemove)
                {
                    user.Roles.Remove(role);
                    _logger.LogDebug("Removed old system role: {RoleName}", role.RoleName);
                }

                // Add new role if not already present
                if (!user.Roles.Any(r => r.RoleName == systemRoleName))
                {
                    user.Roles.Add(targetRole);
                    _logger.LogDebug("Added new system role: {RoleName}", systemRoleName);
                }
            }
            else
            {
                _logger.LogWarning("System role '{SystemRoleName}' not found in app_roles table", systemRoleName);
            }
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Role updated successfully: RoleInStore={RoleInStore}, SystemRole={SystemRole}", dto.RoleInStore, systemRoleName);

        return Ok(new { 
            success = true, 
            message = $"Role updated to {dto.RoleInStore} (system role: {systemRoleName})",
            roleInStore = dto.RoleInStore,
            systemRole = systemRoleName
        });
    }

    /// <summary>
    /// Update user status after successful subscription payment
    /// Called by Saas service when payment is confirmed
    /// </summary>
    [HttpPut("users/{userId}/activate-subscription")]
    public async Task<IActionResult> ActivateUserSubscription(Guid userId)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
            return NotFound(new { success = false, message = "Không tìm thấy người dùng" });

        // Update status from Trial to Active
        user.Status = "Active";
        
        // Clear trial dates since they now have active subscription
        // Keep TrialStartDate for historical purposes
        
        await _db.SaveChangesAsync();

        return Ok(new { 
            success = true, 
            message = "User subscription activated successfully",
            status = user.Status
        });
    }

    /// <summary>
    /// Get user subscription status (for Saas service to check)
    /// </summary>
    [HttpGet("users/{userId}/subscription-status")]
    public async Task<IActionResult> GetUserSubscriptionStatus(Guid userId)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
            return NotFound(new { success = false, message = "Không tìm thấy người dùng" });

        return Ok(new { 
            success = true,
            userId = user.Id,
            status = user.Status,
            trialStartDate = user.TrialStartDate,
            trialEndDate = user.TrialEndDate,
            isTrialExpired = user.Status == "Trial" && user.TrialEndDate.HasValue && user.TrialEndDate.Value <= DateTime.UtcNow
        });
    }
}

public class UpdateRoleDto
{
    [System.Text.Json.Serialization.JsonPropertyName("roleInStore")]
    public string RoleInStore { get; set; } = null!;
}

