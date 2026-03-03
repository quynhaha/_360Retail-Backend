using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Sales.Application.DTOs;

// ===== Request DTOs =====

public class CreateInventoryTicketDto
{
    /// <summary>
    /// "Import" (nhập kho) or "Export" (xuất kho)
    /// </summary>
    [Required(ErrorMessage = "Loại phiếu là bắt buộc")]
    [RegularExpression("^(Import|Export)$", ErrorMessage = "Type phải là Import hoặc Export")]
    public string Type { get; set; } = "Import";
    public string? Note { get; set; }

    [Required(ErrorMessage = "Danh sách sản phẩm là bắt buộc")]
    [MinLength(1, ErrorMessage = "Phiếu kho phải có ít nhất 1 sản phẩm")]
    public List<CreateInventoryTicketItemDto> Items { get; set; } = new();
}

public class CreateInventoryTicketItemDto
{
    [Required(ErrorMessage = "ProductId là bắt buộc")]
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public int Quantity { get; set; }
    public string? Note { get; set; }
}

// ===== Response DTOs =====

public class InventoryTicketDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string? Type { get; set; }
    public string Status { get; set; } = "Draft";
    public int TotalQuantity { get; set; }
    public string? Note { get; set; }
    public Guid? CreatedByEmployeeId { get; set; }
    public Guid? ConfirmedByEmployeeId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public List<InventoryTicketItemDto> Items { get; set; } = new();
}

public class InventoryTicketItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string? Sku { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
}
