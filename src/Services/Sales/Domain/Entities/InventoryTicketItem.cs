using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _360Retail.Services.Sales.Domain.Entities;

[Table("inventory_ticket_items", Schema = "sales")]
public class InventoryTicketItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("ticket_id")]
    public Guid TicketId { get; set; }

    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Column("product_variant_id")]
    public Guid? ProductVariantId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("note")]
    [StringLength(500)]
    public string? Note { get; set; }

    // Navigation
    [ForeignKey("TicketId")]
    public virtual InventoryTicket Ticket { get; set; } = null!;

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("ProductVariantId")]
    public virtual ProductVariant? ProductVariant { get; set; }
}
