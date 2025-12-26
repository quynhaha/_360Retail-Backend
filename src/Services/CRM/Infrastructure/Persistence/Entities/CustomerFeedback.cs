using System;
using System.Collections.Generic;

namespace _360Retail.Services.CRM.Infrastructure.Persistence.Entities;

public partial class CustomerFeedback
{
    public Guid Id { get; set; }

    public Guid StoreId { get; set; }

    public Guid CustomerId { get; set; }

    public string? Content { get; set; }

    public int? Rating { get; set; }

    public string? Source { get; set; }

    public Guid? CreatedByEmployeeId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
