namespace _360Retail.Services.Sales.Application.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string Code { get; set; } = null!;
    public Guid? EmployeeId { get; set; }
    public Guid? CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public string? Status { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime? CreatedAt { get; set; }
    
    public List<OrderItemDto> OrderItems { get; set; } = new();
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? BarCode { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    
    public Guid? ProductVariantId { get; set; }
    public string? Sku { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
}
