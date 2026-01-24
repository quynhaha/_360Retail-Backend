namespace _360Retail.Services.HR.Application.DTOs;

/// <summary>
/// Response DTO for task data
/// </summary>
public class TaskDto
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Guid AssigneeId { get; set; }
    public string Title { get; set; } = null!;
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Description { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool IsActive { get; set; }
    
    // Include employee info for display
    public string? AssigneeName { get; set; }
    public string? AssigneePosition { get; set; }
}
