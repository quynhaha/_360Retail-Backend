using _360Retail.Services.Sales.Infrastructure.Persistence;
using _360Retail.Shared.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _360Retail.Services.Sales.API.Controllers;

/// <summary>
/// Kiểm tra và gửi cảnh báo tồn kho qua email
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize(Roles = "StoreOwner,Manager")]
public class NotificationsController : ControllerBase
{
    private readonly SalesDbContext _db;
    private readonly IEmailSender _emailSender;

    public NotificationsController(SalesDbContext db, IEmailSender emailSender)
    {
        _db = db;
        _emailSender = emailSender;
    }

    /// <summary>
    /// Kiểm tra sản phẩm sắp hết hàng và gửi email cảnh báo cho store owner
    /// </summary>
    [HttpPost("low-stock-check")]
    public async Task<IActionResult> CheckLowStock([FromQuery] int threshold = 10)
    {
        var storeId = User.FindFirst("store_id")?.Value;
        if (string.IsNullOrEmpty(storeId))
            return BadRequest(new { message = "Store context required" });

        var storeGuid = Guid.Parse(storeId);

        // Find low stock products
        var lowStockProducts = await _db.Products
            .Where(p => p.StoreId == storeGuid && p.StockQuantity <= threshold)
            .OrderBy(p => p.StockQuantity)
            .Take(20)
            .Select(p => new
            {
                p.Id,
                p.ProductName,
                p.BarCode,
                p.StockQuantity
            })
            .ToListAsync();

        if (!lowStockProducts.Any())
            return Ok(new { message = "Tất cả sản phẩm đều đủ hàng", count = 0 });

        // Get store owner email from JWT claims
        var ownerEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(ownerEmail))
            return BadRequest(new { message = "Cannot determine owner email" });

        // Get store name
        var storeName = await _db.Database
            .SqlQueryRaw<string>(@"SELECT store_name AS ""Value"" FROM saas.stores WHERE id = {0}", storeGuid)
            .FirstOrDefaultAsync() ?? "360Retail Store";

        // Build email with branded template
        var items = lowStockProducts.Select(p => new LowStockItem
        {
            ProductName = p.ProductName,
            Sku = p.BarCode,
            CurrentStock = p.StockQuantity
        }).ToList();

        var html = EmailTemplateService.LowStockAlert(storeName, items);
        await _emailSender.SendAsync(ownerEmail, $"[360Retail] ⚠️ Cảnh báo tồn kho - {storeName}", html);

        return Ok(new
        {
            message = $"Đã gửi cảnh báo tồn kho qua email {ownerEmail}",
            count = lowStockProducts.Count,
            products = lowStockProducts
        });
    }
}
