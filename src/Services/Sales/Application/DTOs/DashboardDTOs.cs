namespace _360Retail.Services.Sales.Application.DTOs;

// ===== Overview =====
public class DashboardOverviewDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalProducts { get; set; }
    public decimal AvgOrderValue { get; set; }

    // Comparison with previous period (%)
    public decimal? RevenueGrowth { get; set; }
    public decimal? OrderGrowth { get; set; }
}

// ===== Revenue Chart =====
public class RevenueChartDto
{
    public List<RevenueDataPoint> DataPoints { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public string GroupBy { get; set; } = "day";
}

public class RevenueDataPoint
{
    public string Label { get; set; } = "";   // "2026-02-01", "Week 5", "Feb 2026"
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

// ===== Top Products =====
public class TopProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

// ===== Order Status =====
public class OrderStatusSummaryDto
{
    public List<OrderStatusItem> Statuses { get; set; } = new();
    public int TotalOrders { get; set; }
}

public class OrderStatusItem
{
    public string Status { get; set; } = "";
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

// ===== Inventory Summary =====
public class InventorySummaryDto
{
    public int TotalProducts { get; set; }
    public int InStockCount { get; set; }
    public int LowStockCount { get; set; }    // stock <= 10
    public int OutOfStockCount { get; set; }   // stock == 0
    public List<LowStockProductDto> LowStockProducts { get; set; } = new();
}

public class LowStockProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int StockQuantity { get; set; }
    public string? Sku { get; set; }
}

// ===== Recent Activity =====
public class RecentActivityDto
{
    public List<ActivityItem> Activities { get; set; } = new();
}

public class ActivityItem
{
    public string Type { get; set; } = "";        // "Order", "Import", "Export"
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal? Amount { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
}
