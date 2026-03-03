using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Security.Claims;
using System.Text.Json;

namespace _360Retail.Shared.Filters;

/// <summary>
/// Action filter kiểm tra tính năng theo gói subscription.
/// Truy vấn DB lấy features của plan hiện tại → kiểm tra feature flag.
/// Trả 403 nếu tính năng không được phép trong gói hiện tại.
/// </summary>
public class FeatureGateFilter : IAsyncActionFilter
{
    private readonly string _featureName;
    private readonly string _featureDisplayName;

    // Map feature flag → tên hiển thị tiếng Việt + gói tối thiểu
    private static readonly Dictionary<string, (string DisplayName, string MinPlan)> FeatureInfo = new()
    {
        { "has_variants", ("Biến thể sản phẩm", "Basic") },
        { "has_dashboard", ("Dashboard doanh thu", "Basic") },
        { "has_gps_checkin", ("Chấm công GPS", "Pro") },
        { "has_tasks", ("Giao việc/Tasks", "Basic") },
        { "has_feedback_qr", ("Feedback QR Code", "Pro") },
        { "has_loyalty", ("Tích điểm Loyalty", "Pro") },
        { "has_export_excel", ("Export báo cáo Excel", "Pro") },
        { "has_invite_staff", ("Mời nhân viên qua email", "Basic") },
        { "has_multi_store", ("Quản lý đa cửa hàng", "Pro") },
        { "has_realtime_notifications", ("Thông báo realtime", "Basic") },
        { "has_inventory_tickets", ("Phiếu kho nâng cao", "Basic") }
    };

    public FeatureGateFilter(string featureName)
    {
        _featureName = featureName;
        _featureDisplayName = FeatureInfo.TryGetValue(featureName, out var info) 
            ? info.DisplayName 
            : featureName;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Get store_id from JWT claims
        var storeIdClaim = context.HttpContext.User.FindFirst("store_id")?.Value;
        
        if (string.IsNullOrEmpty(storeIdClaim) || !Guid.TryParse(storeIdClaim, out var storeId))
        {
            // No store = can't check features, let other filters handle
            await next();
            return;
        }

        // Query DB for current plan features
        var config = context.HttpContext.RequestServices
            .GetService<Microsoft.Extensions.Configuration.IConfiguration>();

        var connectionString = config?["ConnectionStrings:DefaultConnection"] 
            ?? config?["ConnectionStrings:SaasDb"];

        if (string.IsNullOrEmpty(connectionString))
        {
            // Can't check, allow through
            await next();
            return;
        }

        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            // Get plan features for the store's active subscription
            var sql = @"
                SELECT sp.features, sp.plan_name 
                FROM saas.subscriptions s
                JOIN saas.service_plans sp ON s.plan_id = sp.id
                WHERE s.store_id = @storeId 
                  AND s.is_active = TRUE
                  AND s.end_date > NOW()
                ORDER BY s.end_date DESC
                LIMIT 1";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@storeId", storeId);

            await using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var featuresJson = reader.IsDBNull(0) ? null : reader.GetString(0);
                var planName = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);

                if (!string.IsNullOrEmpty(featuresJson))
                {
                    var features = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(featuresJson);
                    
                    if (features != null && features.TryGetValue(_featureName, out var featureValue))
                    {
                        var isEnabled = featureValue.ValueKind == JsonValueKind.True;
                        
                        if (!isEnabled)
                        {
                            var minPlan = FeatureInfo.TryGetValue(_featureName, out var info) 
                                ? info.MinPlan : "Pro";
                            
                            context.Result = new ObjectResult(new
                            {
                                success = false,
                                error = "FeatureNotAvailable",
                                message = $"Tính năng \"{_featureDisplayName}\" không khả dụng trong gói {planName}. " +
                                          $"Vui lòng nâng cấp lên gói {minPlan} hoặc cao hơn để sử dụng.",
                                currentPlan = planName,
                                requiredPlan = minPlan,
                                feature = _featureName
                            })
                            {
                                StatusCode = 403
                            };
                            return;
                        }
                    }
                }
            }

            await next();
        }
        catch
        {
            // If DB check fails, allow through (fail-open)
            await next();
        }
    }
}

/// <summary>
/// Attribute để gắn lên controller/action — kiểm tra tính năng theo gói.
/// Ví dụ: [RequiresFeature("has_loyalty")] → chặn nếu gói không có loyalty.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequiresFeatureAttribute : TypeFilterAttribute
{
    public RequiresFeatureAttribute(string featureName) : base(typeof(FeatureGateFilter))
    {
        Arguments = new object[] { featureName };
    }
}
