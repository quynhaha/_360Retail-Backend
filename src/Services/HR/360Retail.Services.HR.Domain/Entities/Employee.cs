using System;
using System.Collections.Generic;

namespace _360Retail.Services.HR.Domain.Entities;

public partial class Employee
{
    public Guid Id { get; set; }

    public Guid StoreId { get; set; }

    public Guid AppUserId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Position { get; set; }

    public string? FaceData { get; set; }

    public string? RegisteredDeviceId { get; set; }

    public decimal? BaseSalary { get; set; }

    public DateTime? JoinDate { get; set; }

    public string? Status { get; set; }

    public string? AvatarUrl { get; set; }

    public virtual ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();

    public virtual ICollection<Timekeeping> Timekeepings { get; set; } = new List<Timekeeping>();
}
