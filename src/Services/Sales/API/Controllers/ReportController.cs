using _360Retail.Services.Sales.Application.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace _360Retail.Services.Sales.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IOrderService _orderService;

    public ReportController(IDashboardService dashboardService, IOrderService orderService)
    {
        _dashboardService = dashboardService;
        _orderService = orderService;
    }

    private Guid GetStoreId() =>
        Guid.Parse(User.FindFirstValue("StoreId") ?? throw new UnauthorizedAccessException("Missing StoreId"));

    /// <summary>
    /// Export Sales Revenue Report as Excel (.xlsx)
    /// </summary>
    [HttpGet("sales/export")]
    public async Task<IActionResult> ExportSalesReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var storeId = GetStoreId();
        var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;

        var overview = await _dashboardService.GetOverviewAsync(storeId, fromDate, toDate);
        var topProducts = await _dashboardService.GetTopProductsAsync(storeId, fromDate, toDate, 20);
        var orderStatus = await _dashboardService.GetOrderStatusAsync(storeId, fromDate, toDate);

        using var workbook = new XLWorkbook();

        // Sheet 1: Overview
        var ws1 = workbook.Worksheets.Add("Tổng quan");
        ws1.Cell("A1").Value = "BÁO CÁO DOANH THU";
        ws1.Cell("A1").Style.Font.Bold = true;
        ws1.Cell("A1").Style.Font.FontSize = 16;
        ws1.Cell("A2").Value = $"Từ {fromDate:dd/MM/yyyy} đến {toDate:dd/MM/yyyy}";

        ws1.Cell("A4").Value = "Chỉ số";
        ws1.Cell("B4").Value = "Giá trị";
        ws1.Range("A4:B4").Style.Font.Bold = true;
        ws1.Range("A4:B4").Style.Fill.BackgroundColor = XLColor.LightBlue;

        ws1.Cell("A5").Value = "Tổng doanh thu (VNĐ)";
        ws1.Cell("B5").Value = overview.TotalRevenue;
        ws1.Cell("B5").Style.NumberFormat.Format = "#,##0";
        ws1.Cell("A6").Value = "Tổng đơn hàng";
        ws1.Cell("B6").Value = overview.TotalOrders;
        ws1.Cell("A7").Value = "Giá trị trung bình/đơn";
        ws1.Cell("B7").Value = overview.AvgOrderValue;
        ws1.Cell("B7").Style.NumberFormat.Format = "#,##0";

        ws1.Columns().AdjustToContents();

        // Sheet 2: Top Products
        var ws2 = workbook.Worksheets.Add("Top sản phẩm");
        ws2.Cell("A1").Value = "TOP SẢN PHẨM BÁN CHẠY";
        ws2.Cell("A1").Style.Font.Bold = true;
        ws2.Cell("A1").Style.Font.FontSize = 14;

        ws2.Cell("A3").Value = "#";
        ws2.Cell("B3").Value = "Sản phẩm";
        ws2.Cell("C3").Value = "Số lượng bán";
        ws2.Cell("D3").Value = "Doanh thu (VNĐ)";
        ws2.Range("A3:D3").Style.Font.Bold = true;
        ws2.Range("A3:D3").Style.Fill.BackgroundColor = XLColor.LightGreen;

        for (int i = 0; i < topProducts.Count; i++)
        {
            var row = i + 4;
            ws2.Cell(row, 1).Value = i + 1;
            ws2.Cell(row, 2).Value = topProducts[i].ProductName;
            ws2.Cell(row, 3).Value = topProducts[i].QuantitySold;
            ws2.Cell(row, 4).Value = topProducts[i].Revenue;
            ws2.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
        }

        ws2.Columns().AdjustToContents();

        // Sheet 3: Order Status
        var ws3 = workbook.Worksheets.Add("Trạng thái đơn hàng");
        ws3.Cell("A1").Value = "THỐNG KÊ TRẠNG THÁI ĐƠN HÀNG";
        ws3.Cell("A1").Style.Font.Bold = true;

        ws3.Cell("A3").Value = "Trạng thái";
        ws3.Cell("B3").Value = "Số lượng";
        ws3.Range("A3:B3").Style.Font.Bold = true;
        ws3.Range("A3:B3").Style.Fill.BackgroundColor = XLColor.LightYellow;

        for (int i = 0; i < orderStatus.Statuses.Count; i++)
        {
            ws3.Cell(i + 4, 1).Value = orderStatus.Statuses[i].Status;
            ws3.Cell(i + 4, 2).Value = orderStatus.Statuses[i].Count;
        }

        ws3.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"BaoCaoDoanhThu_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
