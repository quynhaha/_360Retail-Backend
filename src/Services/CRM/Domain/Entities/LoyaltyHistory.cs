using System;
using System.Collections.Generic;

namespace _360Retail.Services.CRM.Domain.Entities;

public partial class LoyaltyHistory
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Guid? OrderId { get; set; }

    public int PointsChanged { get; set; }

    public string? Type { get; set; }

    public DateTime? ChangeDate { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
