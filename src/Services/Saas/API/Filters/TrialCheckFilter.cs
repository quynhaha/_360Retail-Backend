using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace _360Retail.Services.Saas.API.Filters;

/// <summary>
/// Action filter that blocks write operations for users with expired trials.
/// </summary>
public class TrialCheckFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpMethod = context.HttpContext.Request.Method;
        
        var writeOperations = new[] { "POST", "PUT", "DELETE", "PATCH" };
        if (!writeOperations.Contains(httpMethod, StringComparer.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var statusClaim = context.HttpContext.User.FindFirst("status")?.Value;
        
        if (statusClaim == "Trial")
        {
            var trialExpiredClaim = context.HttpContext.User.FindFirst("trial_expired")?.Value;
            
            if (trialExpiredClaim == "true")
            {
                context.Result = new ObjectResult(new
                {
                    success = false,
                    error = "TrialExpired",
                    message = "Thời gian dùng thử đã hết. Vui lòng mua gói để tiếp tục sử dụng."
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
                    success = false,
                    error = "SubscriptionExpired",
                    message = "Gói dịch vụ của bạn đã hết hạn. Vui lòng gia hạn để tiếp tục sử dụng."
                })
                {
                    StatusCode = 403
                };
                return;
            }
        }

        await next();
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequiresActiveSubscriptionAttribute : TypeFilterAttribute
{
    public RequiresActiveSubscriptionAttribute() : base(typeof(TrialCheckFilter))
    {
    }
}
