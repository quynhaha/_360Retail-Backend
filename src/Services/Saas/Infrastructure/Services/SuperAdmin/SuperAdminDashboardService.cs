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

    public async Task<List<AdminStoreDetailDto>> GetAllStoresDetailAsync()
    {
        var stores = await _db.Stores
            .Include(s => s.Subscriptions)
                .ThenInclude(sub => sub.Plan)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        // Cross-schema query: lấy owner email từ identity schema
        var ownerEmails = new Dictionary<Guid, string>();
        try
        {
            using var conn = _db.Database.GetDbConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT usa.store_id, u.email 
                FROM identity.user_store_access usa
                JOIN identity.app_users u ON u.id = usa.user_id
                WHERE usa.role_in_store = 'Owner'";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var storeId = reader.GetGuid(0);
                var email = reader.GetString(1);
                ownerEmails[storeId] = email;
            }
        }
        catch { /* ignore cross-schema errors */ }

        return stores.Select(s =>
        {
            var activeSub = s.Subscriptions
                .Where(sub => sub.Status == "Active" || sub.Status == "Trial" || sub.Status == "Trialing")
                .OrderByDescending(sub => sub.EndDate)
                .FirstOrDefault();

            return new AdminStoreDetailDto
            {
                Id = s.Id,
                StoreName = s.StoreName,
                Address = s.Address,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                OwnerEmail = ownerEmails.GetValueOrDefault(s.Id),
                CurrentPlan = activeSub?.Plan?.PlanName,
                SubscriptionStatus = activeSub?.Status ?? "None",
                SubscriptionEndDate = activeSub?.EndDate
            };
        }).ToList();
    }

    public async Task<List<AdminSubscriptionDto>> GetAllSubscriptionsAsync(string? status, Guid? planId)
    {
        var query = _db.Subscriptions
            .Include(s => s.Store)
            .Include(s => s.Plan)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(s => s.Status == status);

        if (planId.HasValue)
            query = query.Where(s => s.PlanId == planId.Value);

        return await query
            .OrderByDescending(s => s.StartDate)
            .Select(s => new AdminSubscriptionDto
            {
                Id = s.Id,
                StoreName = s.Store.StoreName,
                PlanName = s.Plan.PlanName,
                PlanPrice = s.Plan.Price,
                Status = s.Status,
                StartDate = s.StartDate,
                EndDate = s.EndDate
            })
            .ToListAsync();
    }

    public async Task<List<AdminPaymentDto>> GetAllPaymentsAsync(string? status, DateTime? from, DateTime? to)
    {
        var query = _db.Payments
            .Include(p => p.Subscription)
                .ThenInclude(s => s.Store)
            .Include(p => p.Subscription)
                .ThenInclude(s => s.Plan)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        if (from.HasValue)
            query = query.Where(p => p.PaymentDate >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.PaymentDate <= to.Value);

        return await query
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new AdminPaymentDto
            {
                Id = p.Id,
                StoreName = p.Subscription.Store.StoreName,
                PlanName = p.Subscription.Plan.PlanName,
                Amount = p.Amount,
                Status = p.Status,
                Provider = p.Provider,
                PaymentDate = p.PaymentDate,
                TransactionCode = p.TransactionCode
            })
            .ToListAsync();
    }
}
