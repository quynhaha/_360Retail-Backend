using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Sales.Application.DTOs;

public class ProductVariantDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = null!;
    public string? Size { get; set; }
    public string? Color { get; set; }
    public decimal? PriceOverride { get; set; }
    public int StockQuantity { get; set; }
}

public class CreateProductVariantDto
{
    [Required]
    public string Sku { get; set; } = null!;
    public string? Size { get; set; }
    public string? Color { get; set; }
    public decimal? PriceOverride { get; set; }
    public int StockQuantity { get; set; } = 0;
}

public class UpdateProductVariantDto
{
    public Guid? Id { get; set; } // Null = New Variant
    [Required]
    public string Sku { get; set; } = null!;
    public string? Size { get; set; }
    public string? Color { get; set; }
    public decimal? PriceOverride { get; set; }
    public int StockQuantity { get; set; }
    public bool IsDeleted { get; set; } = false; // Flag to delete
}
