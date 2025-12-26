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

    public virtual Employee Assignee { get; set; } = null!;
}
