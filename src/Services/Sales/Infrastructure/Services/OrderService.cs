using _360Retail.Services.Sales.Application.DTOs;
using _360Retail.Services.Sales.Application.Interfaces;
using _360Retail.Services.Sales.Domain.Entities;
using _360Retail.Services.Sales.Infrastructure.HttpClients;
using _360Retail.Services.Sales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;

namespace _360Retail.Services.Sales.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly SalesDbContext _db;
    private readonly ICrmClient _crmClient;
    private readonly ILogger<OrderService> _logger;
    private readonly IConfiguration _config;

    public OrderService(SalesDbContext db, ICrmClient crmClient, ILogger<OrderService> logger, IConfiguration config)
    {
        _db = db;
        _crmClient = crmClient;
        _logger = logger;
        _config = config;
    }

    private class IdWrapper { public Guid Id { get; set; } }

    public async Task<Guid> CreateAsync(CreateOrderDto dto, Guid storeId, Guid userId)
    {
        // === PLAN LIMIT: max_orders ===
        var planFeatures = await GetPlanFeaturesForStoreAsync(storeId);
        if (planFeatures != null && planFeatures.TryGetValue("max_orders", out var maxOrd) && maxOrd.ValueKind == JsonValueKind.Number)
        {
            var maxOrders = maxOrd.GetInt32();
            if (maxOrders > 0) // -1 = unlimited
            {
                var currentMonth = DateTime.UtcNow;
                var startOfMonth = new DateTime(currentMonth.Year, currentMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var orderCount = await _db.Orders.CountAsync(o => o.StoreId == storeId && o.CreatedAt >= startOfMonth);
                if (orderCount >= maxOrders)
                    throw new Exception($"Đã đạt giới hạn {maxOrders} đơn hàng/tháng của gói hiện tại. Vui lòng nâng cấp gói để tạo thêm đơn hàng.");
            }
        }

        // 1. Resolve EmployeeId from UserId if available (Staff/POS flow)
        var employeeWrapper = await _db.Database
            .SqlQueryRaw<IdWrapper>(
                "SELECT id as \"Id\" FROM hr.employees WHERE app_user_id = {0} AND store_id = {1}", 
                userId, storeId)
            .FirstOrDefaultAsync();

        Guid? employeeId = (employeeWrapper != null && employeeWrapper.Id != Guid.Empty) 
            ? employeeWrapper.Id 
            : null;

        // 2. Validate CustomerId if provided
        Guid? validatedCustomerId = null;
        if (dto.CustomerId.HasValue && dto.CustomerId.Value != Guid.Empty)
        {
            var customerExists = await _db.Database
                .SqlQueryRaw<IdWrapper>(
                    "SELECT id as \"Id\" FROM crm.customers WHERE id = {0} AND store_id = {1}", 
                    dto.CustomerId.Value, storeId)
                .FirstOrDefaultAsync();
            
            if (customerExists == null)
                throw new Exception("Không tìm thấy khách hàng hoặc khách hàng không thuộc cửa hàng");
            
            validatedCustomerId = dto.CustomerId.Value;
        }

        // 3. Validate Products & Stock
        var productIds = dto.Items.Select(x => x.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Include(p => p.ProductVariants) // Include Variants
            .Where(x => productIds.Contains(x.Id) && x.StoreId == storeId)
            .ToListAsync();
            
        // Check if all products exist
        if (products.Count != productIds.Count)
            throw new Exception("Một số sản phẩm không tìm thấy hoặc thuộc cửa hàng khác");

        // 4. Prepare Order
        var order = new Order
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            EmployeeId = employeeId,
            CustomerId = validatedCustomerId,
            Code = GenerateOrderCode(),
            PaymentMethod = dto.PaymentMethod,
            Status = "Completed", 
            PaymentStatus = "Paid", 
            CreatedAt = DateTime.UtcNow,
            TotalAmount = 0,
            DiscountAmount = dto.DiscountAmount
        };
        
        decimal totalAmount = 0;
        
        // 4. Create OrderItems and Deduct Stock
        foreach (var itemDto in dto.Items)
        {
            var product = products.First(p => p.Id == itemDto.ProductId);
            
            // Validation: If product has variants, must select a variant
            if (product.HasVariants && !itemDto.ProductVariantId.HasValue)
            {
                throw new Exception($"Sản phẩm '{product.ProductName}' có biến thể. Vui lòng chọn biến thể.");
            }
            
            decimal finalPrice = product.Price;
            Guid? variantId = null;

            if (itemDto.ProductVariantId.HasValue)
            {
                // Handle Variant
                var variant = product.ProductVariants.FirstOrDefault(v => v.Id == itemDto.ProductVariantId.Value);
                if (variant == null) throw new Exception($"Không tìm thấy biến thể của sản phẩm '{product.ProductName}'");
                
                if (variant.StockQuantity < itemDto.Quantity)
                    throw new Exception($"Không đủ tồn kho cho sản phẩm '{product.ProductName}' (Biến thể: {variant.Sku}). Còn lại: {variant.StockQuantity}");

                if (variant.PriceOverride.HasValue)
                     finalPrice = variant.PriceOverride.Value;

                variant.StockQuantity -= itemDto.Quantity;
                variantId = variant.Id;
            }
            else
            {
                // Handle Base Product
                if (product.StockQuantity < itemDto.Quantity)
                    throw new Exception($"Không đủ tồn kho cho sản phẩm '{product.ProductName}'. Còn lại: {product.StockQuantity}");
                
                product.StockQuantity -= itemDto.Quantity;
            }
            
            var lineTotal = finalPrice * itemDto.Quantity;
            
            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = product.Id,
                ProductVariantId = variantId,
                Quantity = itemDto.Quantity,
                UnitPrice = finalPrice,
                Total = lineTotal
            };
            
            order.OrderItems.Add(orderItem);
            totalAmount += lineTotal;
        }
        
        order.TotalAmount = totalAmount - dto.DiscountAmount;
        if (order.TotalAmount < 0) order.TotalAmount = 0;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // Auto-earn loyalty points (fire-and-forget, won't fail the order)
        if (order.CustomerId.HasValue)
        {
            try
            {
                var totalQty = order.OrderItems.Sum(x => x.Quantity);
                await _crmClient.EarnPointsFromOrderAsync(
                    storeId, order.CustomerId.Value,
                    order.Id, order.TotalAmount, totalQty);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to earn loyalty points for Order {OrderId}", order.Id);
            }
        }

        return order.Id;
    }

    public async Task<PagedResult<OrderDto>> GetListAsync(Guid storeId, Guid userId, string[] userRoles, string? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
         var query = _db.Orders
            .Where(o => o.StoreId == storeId);

         // SECURITY: If user is ONLY a Customer, they should only see their own orders
         if (userRoles.Contains("Customer") && !userRoles.Any(r => r == "Staff" || r == "Manager" || r == "StoreOwner"))
         {
             var customerWrapper = await _db.Database
                .SqlQueryRaw<IdWrapper>(
                    "SELECT id as \"Id\" FROM crm.customers WHERE app_user_id = {0} AND store_id = {1}", 
                    userId, storeId)
                .FirstOrDefaultAsync();

             if (customerWrapper != null)
                query = query.Where(o => o.CustomerId == customerWrapper.Id);
             else
                query = query.Where(o => false); // No customer profile found, return empty
         }

         if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);
            
         if (fromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= fromDate.Value);
            
         if (toDate.HasValue)
             query = query.Where(o => o.CreatedAt <= toDate.Value);
             
         var totalCount = await query.CountAsync();
         var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                Code = o.Code,
                StoreId = o.StoreId,
                EmployeeId = o.EmployeeId,
                CustomerId = o.CustomerId,
                TotalAmount = o.TotalAmount,
                DiscountAmount = o.DiscountAmount,
                Status = o.Status,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();
            
         return new PagedResult<OrderDto>
         {
             Items = items,
             TotalCount = totalCount,
             PageNumber = page,
             PageSize = pageSize
         };
    }
    
    public async Task<OrderDto?> GetByIdAsync(Guid id, Guid storeId, Guid userId, string[] userRoles)
    {
         var query = _db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.ProductVariant)
            .Where(o => o.Id == id && o.StoreId == storeId);

         // SECURITY: If user is ONLY a Customer, they should only see their own order
         if (userRoles.Contains("Customer") && !userRoles.Any(r => r == "Staff" || r == "Manager" || r == "StoreOwner"))
         {
             var customerWrapper = await _db.Database
                .SqlQueryRaw<IdWrapper>(
                    "SELECT id as \"Id\" FROM crm.customers WHERE app_user_id = {0} AND store_id = {1}", 
                    userId, storeId)
                .FirstOrDefaultAsync();

             if (customerWrapper != null)
                query = query.Where(o => o.CustomerId == customerWrapper.Id);
             else
                return null; // No profile, no access
         }

         var order = await query.FirstOrDefaultAsync();
            
         if (order == null) return null;
         
         return new OrderDto
         {
             Id = order.Id,
             Code = order.Code,
             StoreId = order.StoreId,
             EmployeeId = order.EmployeeId,
             CustomerId = order.CustomerId,
             TotalAmount = order.TotalAmount,
             DiscountAmount = order.DiscountAmount,
             Status = order.Status,
             PaymentMethod = order.PaymentMethod,
             PaymentStatus = order.PaymentStatus,
             CreatedAt = order.CreatedAt,
             OrderItems = order.OrderItems.Select(oi => new OrderItemDto
             {
                 Id = oi.Id,
                 ProductId = oi.ProductId,
                 ProductName = oi.Product.ProductName, 
                 BarCode = oi.Product.BarCode,
                 Quantity = oi.Quantity,
                 UnitPrice = oi.UnitPrice,
                 Total = oi.Total,
                 ProductVariantId = oi.ProductVariantId,
                 Sku = oi.ProductVariant?.Sku,
                 Size = oi.ProductVariant?.Size,
                 Color = oi.ProductVariant?.Color
             }).ToList()
         };
    }
    
    // Valid status transitions
    private static readonly Dictionary<string, string[]> ValidTransitions = new()
    {
        { "Pending", new[] { "Completed", "Cancelled" } },
        { "Completed", new[] { "Cancelled" } },
        { "Cancelled", Array.Empty<string>() }
    };

    public async Task UpdateStatusAsync(Guid id, Guid storeId, string status)
    {
         var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.StoreId == storeId);
         if (order == null) throw new Exception("Không tìm thấy đơn hàng");

         // Validate transition
         if (ValidTransitions.TryGetValue(order.Status ?? "Pending", out var allowed))
         {
             if (!allowed.Contains(status))
                 throw new Exception($"Không thể chuyển trạng thái từ '{order.Status}' sang '{status}'");
         }

         // If cancelling, use CancelOrderAsync instead
         if (status == "Cancelled")
         {
             await CancelOrderAsync(id, storeId);
             return;
         }

         order.Status = status;
         await _db.SaveChangesAsync();
    }

    public async Task CancelOrderAsync(Guid id, Guid storeId)
    {
        var order = await _db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.ProductVariant)
            .FirstOrDefaultAsync(o => o.Id == id && o.StoreId == storeId);

        if (order == null)
            throw new Exception("Không tìm thấy đơn hàng");

        if (order.Status == "Cancelled")
            throw new Exception("Đơn hàng đã được hủy rồi");

        // Restore stock for each item
        foreach (var item in order.OrderItems)
        {
            if (item.ProductVariantId.HasValue && item.ProductVariant != null)
            {
                // Restore variant stock
                item.ProductVariant.StockQuantity += item.Quantity;
            }
            else if (item.Product != null)
            {
                // Restore base product stock
                item.Product.StockQuantity += item.Quantity;
            }
        }

        order.Status = "Cancelled";
        order.PaymentStatus = "Refunded";

        await _db.SaveChangesAsync();
    }
    
    private string GenerateOrderCode()
    {
        // Simple generation: ORD-YYMMDD-RANDOM
        return "ORD-" + DateTime.UtcNow.ToString("yyMMdd") + "-" + new Random().Next(1000, 9999);
    }

    private async Task<Dictionary<string, JsonElement>?> GetPlanFeaturesForStoreAsync(Guid storeId)
    {
        try
        {
            var connStr = _config["ConnectionStrings:DefaultConnection"];
            if (string.IsNullOrEmpty(connStr)) return null;
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync();
            var sql = @"SELECT sp.features FROM saas.subscriptions s
                JOIN saas.service_plans sp ON s.plan_id = sp.id
                WHERE s.store_id = @sid AND s.status IN ('Active','Trial')
                AND (s.end_date IS NULL OR s.end_date > NOW())
                ORDER BY s.end_date DESC LIMIT 1";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@sid", storeId);
            var result = await cmd.ExecuteScalarAsync();
            if (result is string json)
                return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get plan features for store {StoreId}", storeId);
        }
        return null;
    }
}
