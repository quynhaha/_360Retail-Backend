using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _360Retail.Services.Sales.Domain.Entities;

[Table("products", Schema = "sales")]
public partial class Product
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("store_id")]
    public Guid StoreId { get; set; }

    [Column("product_name")]
    [StringLength(200)]
    public string ProductName { get; set; } = null!;

    [Column("bar_code")]
    [StringLength(50)]
    public string? BarCode { get; set; }

    [Column("price")]
    [Precision(18, 2)]
    public decimal Price { get; set; }

    [Column("cost_price")]
    [Precision(18, 2)]
    public decimal? CostPrice { get; set; }

    [Column("stock_quantity")]
    public int StockQuantity { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("category_id")]
    public Guid? CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    [InverseProperty("Products")]
    public virtual Category? Category { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("StoreId")]
    public virtual Store? Store { get; set; }

    [InverseProperty("Product")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [InverseProperty("Product")]
    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
}
