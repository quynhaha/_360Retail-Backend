using System;
using System.Collections.Generic;

namespace _360Retail.Services.Sales.Infrastructure.Persistence.Entities;

public partial class Order
{
    public Guid Id { get; set; }

    public Guid StoreId { get; set; }

    public string Code { get; set; } = null!;

    public Guid EmployeeId { get; set; }

    public Guid? CustomerId { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal? DiscountAmount { get; set; }

    public string? Status { get; set; }

    public string? PaymentMethod { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
