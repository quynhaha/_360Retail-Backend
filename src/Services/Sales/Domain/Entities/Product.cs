using System;
using System.Collections.Generic;
namespace _360Retail.Services.Sales.Domain.Entities;

public partial class Product
{
    public Guid Id { get; set; }

    public Guid StoreId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? BarCode { get; set; }

    public decimal Price { get; set; }

    public decimal? CostPrice { get; set; }

    public int? StockQuantity { get; set; }

    public string? ImageUrl { get; set; }

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
