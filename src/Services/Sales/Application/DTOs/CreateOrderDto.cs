using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Sales.Application.DTOs;

public class CreateOrderDto
{
    public Guid? CustomerId { get; set; }
    
    public string? PaymentMethod { get; set; }
    
    public decimal DiscountAmount { get; set; } = 0;

    [Required]
    [MinLength(1, ErrorMessage = "Order must have at least one item")]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class CreateOrderItemDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }

    public Guid? ProductVariantId { get; set; }
}
