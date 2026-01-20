using System;
using System.Collections.Generic;

namespace _360Retail.Services.HR.Domain.Entities;

public partial class WorkTask
{
    public Guid Id { get; set; }

    public Guid StoreId { get; set; }

    public string Title { get; set; } = null!;

    public Guid AssigneeId { get; set; }

    public string? Status { get; set; }

    public string? Priority { get; set; }

    public string? Description { get; set; }

    public DateTime? Deadline { get; set; }

    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Employee who created this task (for permission check)
    /// </summary>
    public Guid? CreatedByEmployeeId { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Employee Assignee { get; set; } = null!;
    
    public virtual Employee? CreatedBy { get; set; }
}
