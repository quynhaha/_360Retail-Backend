using _360Retail.Services.Sales.Application.DTOs;
using _360Retail.Services.Sales.Application.Interfaces;
using _360Retail.Services.Sales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace _360Retail.Services.Sales.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly SalesDbContext _db;

    public DashboardService(SalesDbContext db)
    {
        _db = db;
    }

    // Helper for cross-schema count
    private class CountWrapper { public int Count { get; set; } }

    public async Task<DashboardOverviewDto> GetOverviewAsync(Guid storeId, DateTime from, DateTime to)
    {
        var orders = _db.Orders
            .Where(o => o.StoreId == storeId && o.CreatedAt >= from && o.CreatedAt <= to);

        var totalRevenue = await orders
            .Where(o => o.Status != "Cancelled")
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        var totalOrders = await orders.CountAsync();

        var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        // Count customers from crm schema
        var customerCount = await _db.Database
            .SqlQueryRaw<CountWrapper>(
                "SELECT COUNT(*)::int as \"Count\" FROM crm.customers WHERE store_id = {0}", storeId)
            .FirstOrDefaultAsync();

        var totalProducts = await _db.Products
            .Where(p => p.StoreId == storeId && p.IsActive == true)
            .CountAsync();

        // Previous period comparison
        var periodLength = (to - from).TotalDays;
        var prevFrom = from.AddDays(-periodLength);
        var prevTo = from;

        var prevRevenue = await _db.Orders
            .Where(o => o.StoreId == storeId && o.CreatedAt >= prevFrom && o.CreatedAt <= prevTo && o.Status != "Cancelled")
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        var prevOrders = await _db.Orders
            .Where(o => o.StoreId == storeId && o.CreatedAt >= prevFrom && o.CreatedAt <= prevTo)
            .CountAsync();

        return new DashboardOverviewDto
        {
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            TotalCustomers = customerCount?.Count ?? 0,
            TotalProducts = totalProducts,
            AvgOrderValue = Math.Round(avgOrderValue, 0),
            RevenueGrowth = prevRevenue > 0
                ? Math.Round((totalRevenue - prevRevenue) / prevRevenue * 100, 1)
                : null,
            OrderGrowth = prevOrders > 0
                ? Math.Round((decimal)(totalOrders - prevOrders) / prevOrders * 100, 1)
                : null
        };
    }

    public async Task<RevenueChartDto> GetRevenueChartAsync(Guid storeId, DateTime from, DateTime to, string groupBy)
    {
        var orders = await _db.Orders
            .Where(o => o.StoreId == storeId
                && o.CreatedAt >= from && o.CreatedAt <= to
                && o.Status != "Cancelled")
            .Select(o => new { o.CreatedAt, o.TotalAmount })
            .ToListAsync();

        var dataPoints = groupBy.ToLower() switch
        {
            "week" => orders
                .GroupBy(o => new { Year = o.CreatedAt!.Value.Year, Week = GetIso8601WeekOfYear(o.CreatedAt!.Value) })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Week)
                .Select(g => new RevenueDataPoint
                {
                    Label = $"W{g.Key.Week}/{g.Key.Year}",
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                }).ToList(),

            "month" => orders
                .GroupBy(o => new { o.CreatedAt!.Value.Year, o.CreatedAt!.Value.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new RevenueDataPoint
                {
                    Label = $"{g.Key.Month:D2}/{g.Key.Year}",
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                }).ToList(),

            _ => orders // "day" default
                .GroupBy(o => o.CreatedAt!.Value.Date)
                .OrderBy(g => g.Key)
                .Select(g => new RevenueDataPoint
                {
                    Label = g.Key.ToString("yyyy-MM-dd"),
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                }).ToList()
        };

        return new RevenueChartDto
        {
            DataPoints = dataPoints,
            TotalRevenue = dataPoints.Sum(d => d.Revenue),
            GroupBy = groupBy
        };
    }

    public async Task<List<TopProductDto>> GetTopProductsAsync(Guid storeId, DateTime from, DateTime to, int top)
    {
        return await _db.OrderItems
            .Include(oi => oi.Product)
            .Include(oi => oi.Order)
            .Where(oi => oi.Order.StoreId == storeId
                && oi.Order.CreatedAt >= from && oi.Order.CreatedAt <= to
                && oi.Order.Status != "Cancelled")
            .GroupBy(oi => new { oi.ProductId, oi.Product.ProductName, oi.Product.ImageUrl })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName ?? "",
                ImageUrl = g.Key.ImageUrl,
                QuantitySold = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.Total)
            })
            .OrderByDescending(p => p.Revenue)
            .Take(top)
            .ToListAsync();
    }

    public async Task<OrderStatusSummaryDto> GetOrderStatusAsync(Guid storeId, DateTime from, DateTime to)
    {
        var statuses = await _db.Orders
            .Where(o => o.StoreId == storeId && o.CreatedAt >= from && o.CreatedAt <= to)
            .GroupBy(o => o.Status ?? "Unknown")
            .Select(g => new OrderStatusItem
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        var total = statuses.Sum(s => s.Count);
        foreach (var s in statuses)
        {
            s.Percentage = total > 0 ? Math.Round((decimal)s.Count / total * 100, 1) : 0;
        }

        return new OrderStatusSummaryDto
        {
            Statuses = statuses,
            TotalOrders = total
        };
    }

    public async Task<InventorySummaryDto> GetInventorySummaryAsync(Guid storeId)
    {
        var products = await _db.Products
            .Where(p => p.StoreId == storeId && p.IsActive == true)
            .Select(p => new { p.Id, p.ProductName, p.StockQuantity, p.BarCode })
            .ToListAsync();

        var outOfStock = products.Where(p => p.StockQuantity <= 0).ToList();
        var lowStock = products.Where(p => p.StockQuantity > 0 && p.StockQuantity <= 10).ToList();

        return new InventorySummaryDto
        {
            TotalProducts = products.Count,
            InStockCount = products.Count(p => p.StockQuantity > 10),
            LowStockCount = lowStock.Count,
            OutOfStockCount = outOfStock.Count,
            LowStockProducts = lowStock.Concat(outOfStock)
                .OrderBy(p => p.StockQuantity)
                .Take(20)
                .Select(p => new LowStockProductDto
                {
                    ProductId = p.Id,
                    ProductName = p.ProductName ?? "",
                    StockQuantity = p.StockQuantity,
                    Sku = p.BarCode
                }).ToList()
        };
    }

    public async Task<RecentActivityDto> GetRecentActivityAsync(Guid storeId, int limit)
    {
        // Recent orders
        var recentOrders = await _db.Orders
            .Where(o => o.StoreId == storeId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(limit)
            .Select(o => new ActivityItem
            {
                Type = "Order",
                Code = o.Code,
                Description = $"Đơn hàng {o.Code} - {o.PaymentMethod}",
                Amount = o.TotalAmount,
                Status = o.Status,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        // Recent inventory tickets
        var recentTickets = new List<ActivityItem>();
        try
        {
            recentTickets = await _db.InventoryTickets
                .Where(t => t.StoreId == storeId && !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .Take(limit)
                .Select(t => new ActivityItem
                {
                    Type = t.Type ?? "Inventory",
                    Code = t.Code ?? "",
                    Description = $"Phiếu {t.Type} {t.Code} - {t.TotalQuantity} sản phẩm",
                    Amount = null,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();
        }
        catch
        {
            // Fallback if is_deleted column doesn't exist yet
            recentTickets = await _db.InventoryTickets
                .Where(t => t.StoreId == storeId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(limit)
                .Select(t => new ActivityItem
                {
                    Type = t.Type ?? "Inventory",
                    Code = t.Code ?? "",
                    Description = $"Phiếu {t.Type} {t.Code} - {t.TotalQuantity} sản phẩm",
                    Amount = null,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();
        }

        return new RecentActivityDto
        {
            Activities = recentOrders.Concat(recentTickets)
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .ToList()
        };
    }

    private static int GetIso8601WeekOfYear(DateTime date)
    {
        var day = System.Globalization.CultureInfo.InvariantCulture.Calendar
            .GetDayOfWeek(date);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            date = date.AddDays(3);
        return System.Globalization.CultureInfo.InvariantCulture.Calendar
            .GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }
}
