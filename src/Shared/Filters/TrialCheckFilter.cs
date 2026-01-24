using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace _360Retail.Shared.Filters;

/// <summary>
/// Action filter that blocks write operations (POST, PUT, DELETE) for users with expired trials.
/// Apply this to controllers/actions that should be restricted for expired trial users.
/// </summary>
public class TrialCheckFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpMethod = context.HttpContext.Request.Method;
        
        // Only check for write operations
        var writeOperations = new[] { "POST", "PUT", "DELETE", "PATCH" };
        if (!writeOperations.Contains(httpMethod, StringComparer.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        // Get user status from JWT claims
        var statusClaim = context.HttpContext.User.FindFirst("status")?.Value;
        
        // If status is "Trial", check if trial is still active
        if (statusClaim == "Trial")
        {
            // Check trial_end_date from a separate claim or query DB
            // For now, we'll rely on a custom claim "trial_expired" that should be set during login
            var trialExpiredClaim = context.HttpContext.User.FindFirst("trial_expired")?.Value;
            
            if (trialExpiredClaim == "true")
            {
                context.Result = new ObjectResult(new
                {
                    error = "TrialExpired",
                    message = "Thời gian dùng thử đã hết. Vui lòng mua gói để tiếp tục sử dụng.",
                    message_en = "Your trial period has expired. Please purchase a plan to continue."
                })
                {
                    StatusCode = 403
                };
                return;
            }
        }

        // If status is "Active" (Paid user), check if subscription has expired
        if (statusClaim == "Active")
        {
            var subscriptionExpiredClaim = context.HttpContext.User.FindFirst("subscription_expired")?.Value;
            
            if (subscriptionExpiredClaim == "true")
            {
                context.Result = new ObjectResult(new
                {
                    error = "SubscriptionExpired",
                    message = "Gói dịch vụ của bạn đã hết hạn. Vui lòng gia hạn để tiếp tục sử dụng.",
                    message_en = "Your subscription has expired. Please renew to continue."
                })
                {
                    StatusCode = 403
                };
                return;
            }
        }

        // User is active or trial/subscription is still valid
        await next();
    }
}

/// <summary>
/// Attribute to apply TrialCheckFilter to a controller or action
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequiresActiveSubscriptionAttribute : TypeFilterAttribute
{
    public RequiresActiveSubscriptionAttribute() : base(typeof(TrialCheckFilter))
    {
    }
}
