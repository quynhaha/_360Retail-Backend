using _360Retail.Services.HR.Application.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace _360Retail.Services.HR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly ITimekeepingService _timekeepingService;
    private readonly IEmployeeService _employeeService;

    public ReportController(ITimekeepingService timekeepingService, IEmployeeService employeeService)
    {
        _timekeepingService = timekeepingService;
        _employeeService = employeeService;
    }

    private Guid GetStoreId() =>
        Guid.Parse(User.FindFirstValue("StoreId") ?? throw new UnauthorizedAccessException("Missing StoreId"));

    /// <summary>
    /// Export Monthly Timekeeping Report as Excel (.xlsx)
    /// </summary>
    [HttpGet("timekeeping/export")]
    public async Task<IActionResult> ExportTimekeepingReport(
        [FromQuery] int? month,
        [FromQuery] int? year)
    {
        var storeId = GetStoreId();
        var m = month ?? DateTime.UtcNow.Month;
        var y = year ?? DateTime.UtcNow.Year;

        var summary = await _timekeepingService.GetSummaryAsync(storeId, m, y);
        var employees = await _employeeService.GetAllByStoreIdAsync(storeId, includeInactive: true);

        using var workbook = new XLWorkbook();

        // Sheet 1: Timekeeping Summary
        var ws1 = workbook.Worksheets.Add("Chấm công tháng");
        ws1.Cell("A1").Value = $"BÁO CÁO CHẤM CÔNG THÁNG {m}/{y}";
        ws1.Cell("A1").Style.Font.Bold = true;
        ws1.Cell("A1").Style.Font.FontSize = 16;

        var headers = new[] { "#", "Nhân viên", "Tổng ngày công", "Ngày đi trễ", "Tổng giờ làm", "TB giờ/ngày" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws1.Cell(3, i + 1).Value = headers[i];
            ws1.Cell(3, i + 1).Style.Font.Bold = true;
            ws1.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        for (int i = 0; i < summary.Count; i++)
        {
            var row = i + 4;
            ws1.Cell(row, 1).Value = i + 1;
            ws1.Cell(row, 2).Value = summary[i].EmployeeName;
            ws1.Cell(row, 3).Value = summary[i].TotalDays;
            ws1.Cell(row, 4).Value = summary[i].LateDays;
            ws1.Cell(row, 5).Value = Math.Round(summary[i].TotalHours, 1);
            ws1.Cell(row, 6).Value = Math.Round(summary[i].AvgHoursPerDay, 1);

            // Highlight late days > 3
            if (summary[i].LateDays > 3)
                ws1.Cell(row, 4).Style.Font.FontColor = XLColor.Red;
        }

        // Total row
        var totalRow = summary.Count + 4;
        ws1.Cell(totalRow, 1).Value = "";
        ws1.Cell(totalRow, 2).Value = "TỔNG CỘNG";
        ws1.Cell(totalRow, 2).Style.Font.Bold = true;
        ws1.Cell(totalRow, 3).Value = summary.Sum(s => s.TotalDays);
        ws1.Cell(totalRow, 4).Value = summary.Sum(s => s.LateDays);
        ws1.Cell(totalRow, 5).Value = Math.Round(summary.Sum(s => s.TotalHours), 1);
        ws1.Range(totalRow, 1, totalRow, 6).Style.Fill.BackgroundColor = XLColor.LightGray;

        ws1.Columns().AdjustToContents();

        // Sheet 2: Employee List
        var ws2 = workbook.Worksheets.Add("Danh sách nhân viên");
        ws2.Cell("A1").Value = "DANH SÁCH NHÂN VIÊN";
        ws2.Cell("A1").Style.Font.Bold = true;
        ws2.Cell("A1").Style.Font.FontSize = 14;

        var empHeaders = new[] { "#", "Họ tên", "Chức vụ", "Lương cơ bản", "Ngày vào làm", "Trạng thái" };
        for (int i = 0; i < empHeaders.Length; i++)
        {
            ws2.Cell(3, i + 1).Value = empHeaders[i];
            ws2.Cell(3, i + 1).Style.Font.Bold = true;
            ws2.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
        }

        for (int i = 0; i < employees.Count; i++)
        {
            var row = i + 4;
            ws2.Cell(row, 1).Value = i + 1;
            ws2.Cell(row, 2).Value = employees[i].FullName;
            ws2.Cell(row, 3).Value = employees[i].Position ?? "—";
            ws2.Cell(row, 4).Value = employees[i].BaseSalary ?? 0;
            ws2.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
            ws2.Cell(row, 5).Value = employees[i].JoinDate?.ToString("dd/MM/yyyy") ?? "—";
            ws2.Cell(row, 6).Value = employees[i].IsActive ? "Đang làm" : "Nghỉ việc";

            if (!employees[i].IsActive)
                ws2.Row(row).Style.Font.FontColor = XLColor.Gray;
        }

        ws2.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"BaoCaoChamCong_T{m}_{y}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
