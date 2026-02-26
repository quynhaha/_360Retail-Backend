using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _360Retail.Services.Sales.Domain.Entities;

[Table("inventory_tickets", Schema = "sales")]
public partial class InventoryTicket
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("store_id")]
    public Guid StoreId { get; set; }

    [Column("code")]
    [StringLength(50)]
    public string? Code { get; set; }

    /// <summary>
    /// Type: "Import" (nhập kho) or "Export" (xuất kho)
    /// </summary>
    [Column("type")]
    [StringLength(50)]
    public string? Type { get; set; }

    /// <summary>
    /// Status: "Draft", "Confirmed", "Cancelled"
    /// </summary>
    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    [Column("total_quantity")]
    public int TotalQuantity { get; set; }

    [Column("created_by_employee_id")]
    public Guid? CreatedByEmployeeId { get; set; }

    [Column("confirmed_by_employee_id")]
    public Guid? ConfirmedByEmployeeId { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column("confirmed_at", TypeName = "timestamp without time zone")]
    public DateTime? ConfirmedAt { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public virtual ICollection<InventoryTicketItem> Items { get; set; } = new List<InventoryTicketItem>();
}
