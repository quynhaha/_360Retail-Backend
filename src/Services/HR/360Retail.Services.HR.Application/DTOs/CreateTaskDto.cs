using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.HR.Application.DTOs;

/// <summary>
/// DTO for creating a new task
/// </summary>
public class CreateTaskDto
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;
    
    [Required]
    public Guid AssigneeId { get; set; }
    
    /// <summary>
    /// Priority: Low, Medium, High (default: Medium)
    /// </summary>
    [MaxLength(20)]
    public string? Priority { get; set; }
    
    public string? Description { get; set; }
    
    public DateTime? Deadline { get; set; }
}
