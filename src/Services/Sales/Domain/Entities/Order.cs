using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _360Retail.Services.Sales.Domain.Entities;

[Table("orders", Schema = "sales")]
public partial class Order
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("store_id")]
    public Guid StoreId { get; set; }

    [Column("code")]
    [StringLength(50)]
    public string Code { get; set; } = null!;

    [Column("employee_id")]
    public Guid EmployeeId { get; set; }

    [Column("customer_id")]
    public Guid? CustomerId { get; set; }

    [Column("total_amount")]
    [Precision(18, 2)]
    public decimal TotalAmount { get; set; }

    [Column("discount_amount")]
    [Precision(18, 2)]
    public decimal? DiscountAmount { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string? Status { get; set; }

    [Column("payment_method")]
    [StringLength(50)]
    public string? PaymentMethod { get; set; }

    [Column("payment_status")]
    [StringLength(50)]
    public string? PaymentStatus { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [InverseProperty("Order")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
