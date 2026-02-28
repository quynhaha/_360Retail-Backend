using _360Retail.Services.Identity.Infrastructure.Services;

namespace _360Retail.Services.Identity.API.Middleware;

/// <summary>
/// Middleware kiểm tra mỗi request: nếu token đã bị logout (nằm trong Redis blacklist)
/// thì trả 401 Unauthorized ngay, không cho truy cập tiếp.
/// </summary>
public class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TokenBlacklistService blacklist)
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader["Bearer ".Length..];

            if (await blacklist.IsBlacklistedAsync(token))
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"message\":\"Token has been revoked. Please login again.\"}");
                return;
            }
        }

        await _next(context);
    }
}
