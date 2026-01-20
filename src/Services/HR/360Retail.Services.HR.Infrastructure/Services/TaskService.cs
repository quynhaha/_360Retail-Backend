using _360Retail.Services.HR.Application.DTOs;
using _360Retail.Services.HR.Application.Interfaces;
using _360Retail.Services.HR.Domain.Entities;
using _360Retail.Services.HR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace _360Retail.Services.HR.Infrastructure.Services;

public class TaskService : ITaskService
{
    private readonly HrDbContext _db;
    private readonly IEmailService _emailService;
    private readonly HttpClient _identityClient;

    public TaskService(HrDbContext db, IEmailService emailService, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _emailService = emailService;
        _identityClient = httpClientFactory.CreateClient("IdentityService");
    }

    /// <summary>
    /// Create a new task and assign to an employee
    /// Validates hierarchy: Owner can assign to Manager/Staff, Manager can assign to Staff only
    /// </summary>
    public async Task<TaskDto> CreateAsync(CreateTaskDto dto, Guid storeId, Guid creatorAppUserId, string[] creatorRoles)
    {
        var isOwner = creatorRoles.Any(r => r.Equals("StoreOwner", StringComparison.OrdinalIgnoreCase) 
                                         || r.Equals("Owner", StringComparison.OrdinalIgnoreCase));

        // Get creator employee record (required for Manager, optional for Owner)
        var creator = await _db.Employees
            .FirstOrDefaultAsync(e => e.AppUserId == creatorAppUserId && e.StoreId == storeId && e.IsActive);
        
        // Manager MUST have employee record, Owner can create without it
        if (!isOwner && creator == null)
            throw new Exception("You are not an employee in this store");

        // Get the assignee employee
        var assignee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == dto.AssigneeId && e.StoreId == storeId && e.IsActive);
        
        if (assignee == null)
            throw new Exception("Assignee not found in this store or is inactive");

        // Validate hierarchy
        ValidateAssignmentHierarchy(creatorRoles, assignee.Position);

        var task = new WorkTask
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            Title = dto.Title,
            AssigneeId = dto.AssigneeId,
            Status = "Pending",
            Priority = dto.Priority ?? "Medium",
            Description = dto.Description,
            Deadline = dto.Deadline,
            CreatedAt = DateTime.UtcNow,
            CreatedByEmployeeId = creator?.Id,  // null for Owner without employee record
            IsActive = true
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        // Send email notification to assignee (fire and forget, don't block)
        _ = SendTaskAssignmentEmailAsync(assignee, task);

        return MapToDto(task, assignee);
    }

    /// <summary>
    /// Send email notification to assignee when task is created
    /// </summary>
    private async Task SendTaskAssignmentEmailAsync(Employee assignee, WorkTask task)
    {
        try
        {
            // Get assignee email from Identity service
            var email = await GetUserEmailFromIdentity(assignee.AppUserId);
            if (string.IsNullOrEmpty(email))
                return;

            await _emailService.SendTaskAssignmentEmailAsync(
                email,
                assignee.FullName,
                task.Title,
                task.Priority,
                task.Description,
                task.Deadline
            );
        }
        catch
        {
            // Email failure should not fail task creation
        }
    }

    /// <summary>
    /// Get user email from Identity service
    /// </summary>
    private async Task<string?> GetUserEmailFromIdentity(Guid appUserId)
    {
        try
        {
            var response = await _identityClient.GetAsync($"/identity/internal/users/{appUserId}");
            if (response.IsSuccessStatusCode)
            {
                var userInfo = await response.Content.ReadFromJsonAsync<UserInfoResponse>();
                return userInfo?.Email;
            }
        }
        catch
        {
            // Identity service unavailable
        }
        return null;
    }

    private class UserInfoResponse
    {
        public string? Email { get; set; }
    }

    /// <summary>
    /// Get all tasks in a store (for Manager/Owner)
    /// </summary>
    public async Task<List<TaskDto>> GetAllByStoreAsync(Guid storeId, bool includeInactive = false)
    {
        var query = _db.Tasks
            .Include(t => t.Assignee)
            .Where(t => t.StoreId == storeId);

        if (!includeInactive)
            query = query.Where(t => t.IsActive);

        var tasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

        return tasks.Select(t => MapToDto(t, t.Assignee)).ToList();
    }

    /// <summary>
    /// Get tasks assigned to current user's employee record
    /// </summary>
    public async Task<List<TaskDto>> GetMyTasksAsync(Guid storeId, Guid appUserId, bool includeInactive = false)
    {
        // First find the employee record for this user
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.AppUserId == appUserId && e.StoreId == storeId);

        if (employee == null)
            return new List<TaskDto>();

        var query = _db.Tasks
            .Include(t => t.Assignee)
            .Where(t => t.StoreId == storeId && t.AssigneeId == employee.Id);

        if (!includeInactive)
            query = query.Where(t => t.IsActive);

        var tasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

        return tasks.Select(t => MapToDto(t, t.Assignee)).ToList();
    }

    /// <summary>
    /// Get task by ID
    /// </summary>
    public async Task<TaskDto?> GetByIdAsync(Guid taskId, Guid storeId)
    {
        var task = await _db.Tasks
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.StoreId == storeId);

        return task == null ? null : MapToDto(task, task.Assignee);
    }

    /// <summary>
    /// Partial update task (only non-null fields will be updated)
    /// Manager can only update tasks they created. Manager cannot reassign to self.
    /// </summary>
    public async Task<bool> UpdateAsync(Guid taskId, Guid storeId, UpdateTaskDto dto, Guid updaterAppUserId, string[] updaterRoles)
    {
        var task = await _db.Tasks
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.StoreId == storeId);

        if (task == null)
            return false;

        var isOwner = updaterRoles.Any(r => r.Equals("StoreOwner", StringComparison.OrdinalIgnoreCase) 
                                         || r.Equals("Owner", StringComparison.OrdinalIgnoreCase));
        var isManager = updaterRoles.Any(r => r.Equals("Manager", StringComparison.OrdinalIgnoreCase));

        // Get updater's employee record (required for Manager, optional for Owner)
        var updater = await _db.Employees
            .FirstOrDefaultAsync(e => e.AppUserId == updaterAppUserId && e.StoreId == storeId);

        // Manager MUST have employee record, Owner can update without it
        if (!isOwner && updater == null)
            throw new Exception("You are not an employee in this store");

        // Manager can only update tasks THEY created (Owner can update any)
        if (isManager && !isOwner)
        {
            if (task.CreatedByEmployeeId != updater!.Id)
                throw new Exception("Managers can only update tasks they created");
        }

        // Partial update - only update non-null fields
        if (!string.IsNullOrWhiteSpace(dto.Title))
            task.Title = dto.Title;

        if (!string.IsNullOrWhiteSpace(dto.Priority))
            task.Priority = dto.Priority;

        if (dto.Description != null)
            task.Description = dto.Description;

        if (dto.Deadline.HasValue)
            task.Deadline = dto.Deadline.Value;

        // Handle reassignment
        if (dto.AssigneeId.HasValue && dto.AssigneeId.Value != task.AssigneeId)
        {
            // Manager cannot reassign to self
            if (isManager && !isOwner && updater != null && dto.AssigneeId.Value == updater.Id)
                throw new Exception("Managers cannot reassign tasks to themselves");

            var newAssignee = await _db.Employees
                .FirstOrDefaultAsync(e => e.Id == dto.AssigneeId.Value && e.StoreId == storeId && e.IsActive);

            if (newAssignee == null)
                throw new Exception("New assignee not found in this store or is inactive");

            // Validate hierarchy for new assignee
            ValidateAssignmentHierarchy(updaterRoles, newAssignee.Position);

            task.AssigneeId = dto.AssigneeId.Value;
        }

        // Handle restore (set IsActive back to true)
        if (dto.IsActive.HasValue)
            task.IsActive = dto.IsActive.Value;

        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Update task status (for assignee to update their own task progress)
    /// Owner/Manager can update any task status
    /// </summary>
    public async Task<bool> UpdateStatusAsync(Guid taskId, Guid storeId, Guid appUserId, string[] roles, string status)
    {
        var task = await _db.Tasks
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.StoreId == storeId);

        if (task == null)
            return false;

        // Validate status value
        var validStatuses = new[] { "Pending", "InProgress", "Completed", "Cancelled" };
        if (!validStatuses.Contains(status))
            throw new Exception($"Invalid status. Valid values: {string.Join(", ", validStatuses)}");

        var isOwner = roles.Any(r => r.Equals("StoreOwner", StringComparison.OrdinalIgnoreCase) 
                                  || r.Equals("Owner", StringComparison.OrdinalIgnoreCase));
        var isManager = roles.Any(r => r.Equals("Manager", StringComparison.OrdinalIgnoreCase));

        // Owner can update any task status without employee record
        if (isOwner)
        {
            task.Status = status;
            await _db.SaveChangesAsync();
            return true;
        }

        // For Manager/Staff, need to check employee record
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.AppUserId == appUserId && e.StoreId == storeId);

        if (employee == null)
            throw new Exception("You are not an employee in this store");

        // Manager can update any task status in their store
        if (isManager)
        {
            task.Status = status;
            await _db.SaveChangesAsync();
            return true;
        }

        // Staff can only update their own assigned tasks
        if (task.AssigneeId != employee.Id)
            throw new Exception("You can only update status of tasks assigned to you");

        task.Status = status;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Soft delete a task
    /// </summary>
    public async Task<bool> DeleteAsync(Guid taskId, Guid storeId)
    {
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.StoreId == storeId);

        if (task == null)
            return false;

        task.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    #region Private Helpers

    /// <summary>
    /// Validate that the creator can assign to the target position
    /// Owner → Manager, Staff
    /// Manager → Staff only
    /// </summary>
    private void ValidateAssignmentHierarchy(string[] creatorRoles, string? assigneePosition)
    {
        var isOwner = creatorRoles.Any(r => r.Equals("StoreOwner", StringComparison.OrdinalIgnoreCase) 
                                         || r.Equals("Owner", StringComparison.OrdinalIgnoreCase));
        var isManager = creatorRoles.Any(r => r.Equals("Manager", StringComparison.OrdinalIgnoreCase));

        var targetPosition = assigneePosition?.ToLower() ?? "staff";

        if (isOwner)
        {
            // Owner can assign to anyone (Manager or Staff)
            return;
        }

        if (isManager)
        {
            // Manager can only assign to Staff
            if (targetPosition == "manager" || targetPosition == "owner" || targetPosition == "storeowner")
            {
                throw new Exception("Managers can only assign tasks to Staff, not to other Managers or Owners");
            }
            return;
        }

        throw new Exception("Only Owner or Manager can create tasks");
    }

    private TaskDto MapToDto(WorkTask task, Employee? assignee)
    {
        return new TaskDto
        {
            Id = task.Id,
            StoreId = task.StoreId,
            AssigneeId = task.AssigneeId,
            Title = task.Title,
            Status = task.Status,
            Priority = task.Priority,
            Description = task.Description,
            Deadline = task.Deadline,
            CreatedAt = task.CreatedAt,
            IsActive = task.IsActive,
            AssigneeName = assignee?.FullName,
            AssigneePosition = assignee?.Position
        };
    }

    #endregion
}
