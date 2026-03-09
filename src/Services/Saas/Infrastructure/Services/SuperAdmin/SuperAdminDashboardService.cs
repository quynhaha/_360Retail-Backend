using Microsoft.EntityFrameworkCore;
using _360Retail.Services.Saas.Application.DTOs.SuperAdmin;
using _360Retail.Services.Saas.Application.Interfaces.SuperAdmin;
using _360Retail.Services.Saas.Infrastructure.Persistence;

namespace _360Retail.Services.Saas.Infrastructure.Services.SuperAdmin;

public class SuperAdminDashboardService : ISuperAdminDashboardService
{
    private readonly SaasDbContext _db;

    public SuperAdminDashboardService(SaasDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync()
    {
        // 1. Revenue (cast to nullable to avoid Npgsql error on empty table)
        var totalRevenue = await _db.Payments
            .Where(p => p.Status == "Completed")
            .Select(p => (decimal?)p.Amount)
            .SumAsync() ?? 0;

        var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var mrr = await _db.Payments
            .Where(p => p.Status == "Completed" && p.PaymentDate >= firstDayOfMonth)
            .Select(p => (decimal?)p.Amount)
            .SumAsync() ?? 0;

        // 2. Stores Status
        // A store is Active if it has any active subscription
        var activeStores = await _db.Stores
            .CountAsync(s => s.Subscriptions.Any(sub => sub.Status == "Active"));
            
        var trialStores = await _db.Stores
            .CountAsync(s => s.Subscriptions.Any(sub => sub.Status == "Trial" || sub.Status == "Trialing"));

        var expiredStores = await _db.Stores
            .CountAsync(s => s.Subscriptions.All(sub => sub.Status == "Expired" || sub.Status == "Canceled"));

        // 3. Conversion Rate (Trial -> Paid)
        // Find stores that have at least one completed payment
        var paidStoresCount = await _db.Stores
            .CountAsync(s => s.Subscriptions.Any(sub => sub.Payments.Any(p => p.Status == "Completed")));
            
        var totalStoresWithTrial = await _db.Stores
            .CountAsync(s => s.Subscriptions.Any(sub => sub.Status == "Trial" || sub.Status == "Trialing" || sub.Status == "Active" || sub.Status == "Expired"));

        decimal conversionRate = 0;
        if (totalStoresWithTrial > 0)
        {
            conversionRate = Math.Round((decimal)paidStoresCount / totalStoresWithTrial * 100, 2);
        }

        return new DashboardOverviewDto
        {
            TotalRevenue = totalRevenue,
            MonthlyRecurringRevenue = mrr,
            ActiveStores = activeStores,
            TrialStores = trialStores,
            ExpiredStores = expiredStores,
            TrialToPaidConversionRate = conversionRate
        };
    }

    public async Task<List<RevenueStatDto>> GetRevenueChartAsync(DateTime from, DateTime to, string groupBy)
    {
        // Fetch raw data into memory first to avoid EF Core translation issues with GroupBy and string formatting
        var payments = await _db.Payments
            .Where(p => p.Status == "Completed" && p.PaymentDate >= from && p.PaymentDate <= to)
            .Select(p => new { p.PaymentDate, p.Amount })
            .ToListAsync();

        var grouped = groupBy.ToLower() switch
        {
            "month" => payments.GroupBy(p => new { p.PaymentDate!.Value.Year, p.PaymentDate!.Value.Month })
                               .Select(g => new RevenueStatDto { Date = $"{g.Key.Year}-{g.Key.Month:D2}", Revenue = g.Sum(x => x.Amount) }),
            "week" => payments.GroupBy(p => {
                                  var cal = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
                                  return new { p.PaymentDate!.Value.Year, Week = cal.GetWeekOfYear(p.PaymentDate!.Value, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday) };
                              })
                              .Select(g => new RevenueStatDto { Date = $"{g.Key.Year}-W{g.Key.Week:D2}", Revenue = g.Sum(x => x.Amount) }),
            _ => payments.GroupBy(p => p.PaymentDate!.Value.Date) // default to day
                         .Select(g => new RevenueStatDto { Date = g.Key.ToString("yyyy-MM-dd"), Revenue = g.Sum(x => x.Amount) })
        };

        return grouped.OrderBy(r => r.Date).ToList();
    }

    public async Task<List<PlanDistributionDto>> GetPlanDistributionAsync()
    {
        return await _db.Subscriptions
            .Where(s => s.Status == "Active" || s.Status == "Trial" || s.Status == "Trialing")
            .Include(s => s.Plan)
            .GroupBy(s => s.Plan.PlanName)
            .Select(g => new PlanDistributionDto
            {
                PlanName = g.Key,
                Count = g.Count()
            })
            .ToListAsync();
    }
}
