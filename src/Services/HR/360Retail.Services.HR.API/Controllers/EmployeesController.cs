using _360Retail.Services.HR.Application.DTOs;
using _360Retail.Services.HR.Application.Interfaces;
using _360Retail.Services.HR.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace _360Retail.Services.HR.API.Controllers;

[ApiController]
[Route("api/employees")]
[RequiresActiveSubscription]  // Block writes for expired trials
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IStorageService _storageService;

    public EmployeesController(IEmployeeService employeeService, IStorageService storageService)
    {
        _employeeService = employeeService;
        _storageService = storageService;
    }

    #region Internal APIs (called by Identity Service)

    /// <summary>
    /// Internal: Identity calls this to create employee after invite
    /// </summary>
    [AllowAnonymous]
    [HttpPost("internal/create")] // Full path: api/hr/employees/internal/create
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
    {
        try
        {
            var result = await _employeeService.CreateAsync(dto);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Public APIs (for employees to manage their profile)

    /// <summary>
    /// Get current employee's profile
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var appUserId = GetAppUserId();
        var storeId = GetStoreId();

        if (appUserId == null || storeId == null)
            return Unauthorized(new { success = false, message = "Invalid token" });

        var profile = await _employeeService.GetByAppUserIdAsync(appUserId.Value, storeId.Value);

        if (profile == null)
            return NotFound(new { success = false, message = "Employee profile not found" });

        return Ok(new { success = true, data = profile });
    }

    /// <summary>
    /// Update current employee's profile (partial update)
    /// </summary>
    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateEmployeeProfileDto dto)
    {
        var appUserId = GetAppUserId();
        var storeId = GetStoreId();

        if (appUserId == null || storeId == null)
            return Unauthorized(new { success = false, message = "Invalid token" });

        try
        {
            var success = await _employeeService.UpdateProfileAsync(appUserId.Value, storeId.Value, dto);

            if (!success)
                return NotFound(new { success = false, message = "Employee profile not found" });

            return Ok(new { success = true, message = "Profile updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Upload avatar image for current employee
    /// </summary>
    [Authorize]
    [HttpPost("me/avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var appUserId = GetAppUserId();
        var storeId = GetStoreId();

        if (appUserId == null || storeId == null)
            return Unauthorized(new { success = false, message = "Invalid token" });

        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "No file provided" });

        try
        {
            var avatarUrl = await _storageService.SaveFileAsync(file, "employee-avatars");
            var success = await _employeeService.UpdateAvatarAsync(appUserId.Value, storeId.Value, avatarUrl);

            if (!success)
                return NotFound(new { success = false, message = "Employee not found" });

            return Ok(new { success = true, data = new { avatarUrl }, message = "Avatar uploaded successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Manager/Owner APIs

    /// <summary>
    /// Get all employees in current store (for Manager/Owner)
    /// </summary>
    [Authorize(Roles = "Manager,StoreOwner,Owner")]
    [HttpGet]
    public async Task<IActionResult> GetAllEmployees([FromQuery] bool includeInactive = false)
    {
        var storeId = GetStoreId();
        if (storeId == null)
            return Unauthorized(new { success = false, message = "Invalid token - no store assigned" });

        var employees = await _employeeService.GetAllByStoreIdAsync(storeId.Value, includeInactive);
        return Ok(new { success = true, data = employees });
    }

    /// <summary>
    /// Get employee by ID in current store (for Manager/Owner)
    /// </summary>
    [Authorize(Roles = "Manager,StoreOwner,Owner")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEmployeeById(Guid id)
    {
        var storeId = GetStoreId();
        if (storeId == null)
            return Unauthorized(new { success = false, message = "Invalid token - no store assigned" });

        var employee = await _employeeService.GetByIdAsync(id, storeId.Value);
        if (employee == null)
            return NotFound(new { success = false, message = "Employee not found in your store" });

        return Ok(new { success = true, data = employee });
    }

    /// <summary>
    /// Update employee information (OWNER ONLY)
    /// Can update: FullName, Position, BaseSalary, Status
    /// </summary>
    [Authorize(Roles = "StoreOwner,Owner")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeByOwnerDto dto)
    {
        var storeId = GetStoreId();
        if (storeId == null)
            return Unauthorized(new { success = false, message = "Invalid token - no store assigned" });

        try
        {
            var success = await _employeeService.UpdateByOwnerAsync(id, storeId.Value, dto);
            if (!success)
                return NotFound(new { success = false, message = "Employee not found in your store" });

            return Ok(new { success = true, message = "Employee updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Helpers

    private Guid? GetAppUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var id) ? id : null;
    }

    private Guid? GetStoreId()
    {
        var storeId = User.FindFirstValue("store_id");
        return Guid.TryParse(storeId, out var id) ? id : null;
    }

    #endregion
}
