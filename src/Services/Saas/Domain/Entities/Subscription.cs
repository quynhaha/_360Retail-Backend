using System;
using System.Collections.Generic;

namespace _360Retail.Services.Saas.Domain.Entities;

public partial class Subscription
{
    public Guid Id { get; set; }

    public Guid StoreId { get; set; }

    public Guid PlanId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Status { get; set; }

    public bool? AutoRenew { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ServicePlan Plan { get; set; } = null!;

    public virtual Store Store { get; set; } = null!;
}
