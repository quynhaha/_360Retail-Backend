using _360Retail.Services.HR.Application.DTOs;
using _360Retail.Services.HR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace _360Retail.Services.HR.API.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    #region Create Task (Manager/Owner only)

    /// <summary>
    /// Create a new task and assign to an employee
    /// Owner can assign to Manager/Staff, Manager can assign to Staff only
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Manager,StoreOwner,Owner")]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
    {
        var storeId = GetStoreId();
        if (storeId == null)
            return Unauthorized(new { success = false, message = "Invalid token - no store assigned" });

        var appUserId = GetAppUserId();
        if (appUserId == null)
            return Unauthorized(new { success = false, message = "Invalid token" });

        var roles = GetCurrentRoles();

        try
        {
            var result = await _taskService.CreateAsync(dto, storeId.Value, appUserId.Value, roles);
            return Ok(new { success = true, data = result, message = "Task created successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Get Tasks

    /// <summary>
    /// Get all tasks in current store (Manager/Owner only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Manager,StoreOwner,Owner")]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var storeId = GetStoreId();
        if (storeId == null)
            return Unauthorized(new { success = false, message = "Invalid token - no store assigned" });

        var tasks = await _taskService.GetAllByStoreAsync(storeId.Value, includeInactive);
        return Ok(new { success = true, data = tasks });
    }

    /// <summary>
    /// Get tasks assigned to current user
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyTasks([FromQuery] bool includeInactive = false)
    {
        var storeId = GetStoreId();
        if (storeId == null)
            return Unauthorized(new { success = false, message = "Invalid token - no store assigned" });

        var appUserId = GetAppUserId();
        if (appUserId == null)
            return Unauthorized(new { success = false, message = "Invalid token" });

        var tasks = await _taskService.GetMyTasksAsync(storeId.Value, appUserId.Value, includeInactive);
        return Ok(new { success = true, data = tasks });
    }

    /// <summary>
    /// Get task by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var storeId = GetStoreId();
        if (storeId == null)
            return Unauthorized(new { success = false, message = "Invalid token - no store assigned" });

        var task = await _taskService.GetByIdAsync(id, storeId.Value);
        if (task == null)
            return NotFound(new { success = false, message = "Task not found" });

        return Ok(new { success = true, data = task });
    }

    #endregion

    #region Update Task

    /// <summary>
    /// Update task (partial update) - Manager/Owner only
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Manager,StoreOwner,Owner")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskDto dto)
    {
        var storeId = GetStoreId();
        if (storeId == null)
            return Unauthorized(new { success = false, message = "Invalid token - no store assigned" });

        var appUserId = GetAppUserId();
        if (appUserId == null)
            return Unauthorized(new { success = false, message = "Invalid token" });

        var roles = GetCurrentRoles();

        try
        {
            var success = await _taskService.UpdateAsync(id, storeId.Value, dto, appUserId.Value, roles);
            if (!success)
                return NotFound(new { success = false, message = "Task not found" });

            return Ok(new { success = true, message = "Task updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Update task status only (Staff can update their own tasks)
    /// Valid statuses: Pending, InProgress, Completed, Cancelled
    /// </summary>
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return BadRequest(new { success = false, message = "Status is required" });

        var storeId = GetStoreId();
        if (storeId == null)
            return Unauthorized(new { success = false, message = "Invalid token - no store assigned" });

        var appUserId = GetAppUserId();
        if (appUserId == null)
            return Unauthorized(new { success = false, message = "Invalid token" });

        var roles = GetCurrentRoles();

        try
        {
            var success = await _taskService.UpdateStatusAsync(id, storeId.Value, appUserId.Value, roles, status);
            if (!success)
                return NotFound(new { success = false, message = "Task not found" });

            return Ok(new { success = true, message = "Status updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Delete Task (Soft Delete)

    /// <summary>
    /// Soft delete a task - Manager/Owner only
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Manager,StoreOwner,Owner")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var storeId = GetStoreId();
        if (storeId == null)
            return Unauthorized(new { success = false, message = "Invalid token - no store assigned" });

        var success = await _taskService.DeleteAsync(id, storeId.Value);
        if (!success)
            return NotFound(new { success = false, message = "Task not found" });

        return Ok(new { success = true, message = "Task deleted successfully" });
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

    private string[] GetCurrentRoles()
    {
        return User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
    }

    #endregion
}
