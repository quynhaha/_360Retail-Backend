using System;
using System.Collections.Generic;

namespace _360Retail.Services.Saas.Domain.Entities;

public partial class ServicePlan
{
    public Guid Id { get; set; }

    public string PlanName { get; set; } = null!;

    public decimal Price { get; set; }

    public int DurationDays { get; set; }

    public string? Features { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
