using _360Retail.Services.HR.Application.DTOs;

namespace _360Retail.Services.HR.Application.Interfaces;

public interface ITaskService
{
    /// <summary>
    /// Create a new task and assign to an employee
    /// Validates hierarchy: Owner can assign to Manager/Staff, Manager can assign to Staff only
    /// </summary>
    Task<TaskDto> CreateAsync(CreateTaskDto dto, Guid storeId, Guid creatorAppUserId, string[] creatorRoles);
    
    /// <summary>
    /// Get all tasks in a store (for Manager/Owner)
    /// </summary>
    Task<List<TaskDto>> GetAllByStoreAsync(Guid storeId, bool includeInactive = false);
    
    /// <summary>
    /// Get tasks assigned to current user's employee record
    /// </summary>
    Task<List<TaskDto>> GetMyTasksAsync(Guid storeId, Guid appUserId, bool includeInactive = false);
    
    /// <summary>
    /// Get task by ID
    /// </summary>
    Task<TaskDto?> GetByIdAsync(Guid taskId, Guid storeId);
    
    /// <summary>
    /// Partial update task (only non-null fields will be updated)
    /// </summary>
    Task<bool> UpdateAsync(Guid taskId, Guid storeId, UpdateTaskDto dto, Guid updaterAppUserId, string[] updaterRoles);
    
    /// <summary>
    /// Update task status (for assignee to update their own task progress)
    /// Owner/Manager can update any task status
    /// </summary>
    Task<bool> UpdateStatusAsync(Guid taskId, Guid storeId, Guid appUserId, string[] roles, string status);
    
    /// <summary>
    /// Soft delete a task
    /// </summary>
    Task<bool> DeleteAsync(Guid taskId, Guid storeId);
}
