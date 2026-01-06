using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _360Retail.Services.Sales.Domain.Entities;

[Table("product_variants", Schema = "sales")]
public partial class ProductVariant
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Column("sku")]
    [StringLength(50)]
    public string Sku { get; set; } = null!;

    [Column("size")]
    [StringLength(20)]
    public string? Size { get; set; }

    [Column("color")]
    [StringLength(20)]
    public string? Color { get; set; }

    [Column("price_override")]
    [Precision(18, 2)]
    public decimal? PriceOverride { get; set; }

    [Column("stock_quantity")]
    public int StockQuantity { get; set; } = 0;

    [ForeignKey("ProductId")]
    [InverseProperty("ProductVariants")]
    public virtual Product Product { get; set; } = null!;
}
