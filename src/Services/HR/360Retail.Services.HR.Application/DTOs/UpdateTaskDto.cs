using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.HR.Application.DTOs;

/// <summary>
/// DTO for partial update of a task
/// All fields are optional - only provided fields will be updated
/// </summary>
public class UpdateTaskDto
{
    [MaxLength(200)]
    public string? Title { get; set; }
    
    /// <summary>
    /// Reassign to another employee (optional)
    /// </summary>
    public Guid? AssigneeId { get; set; }
    
    /// <summary>
    /// Priority: Low, Medium, High
    /// </summary>
    [MaxLength(20)]
    public string? Priority { get; set; }
    
    public string? Description { get; set; }
    
    public DateTime? Deadline { get; set; }
    
    /// <summary>
    /// Set to true to restore a soft-deleted task
    /// </summary>
    public bool? IsActive { get; set; }
}
