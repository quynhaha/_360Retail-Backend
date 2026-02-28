using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace _360Retail.Shared.Common.Middleware;

/// <summary>
/// Middleware that protects internal API endpoints by requiring a shared API key.
/// Only applies to routes containing "/internal/" in the path.
/// Services must include header "X-Internal-Key" with the correct key.
/// </summary>
public class InternalApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _expectedKey;
    private readonly ILogger<InternalApiKeyMiddleware> _logger;

    public InternalApiKeyMiddleware(
        RequestDelegate next,
        IConfiguration config,
        ILogger<InternalApiKeyMiddleware> logger)
    {
        _next = next;
        _expectedKey = config["InternalApi:Key"] ?? "360retail-internal-secret-key";
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Only check requests that target internal endpoints
        if (path.Contains("/internal/", StringComparison.OrdinalIgnoreCase))
        {
            if (!context.Request.Headers.TryGetValue("X-Internal-Key", out var providedKey)
                || providedKey.ToString() != _expectedKey)
            {
                _logger.LogWarning(
                    "Unauthorized internal API call to {Path} from {IP}",
                    path, context.Connection.RemoteIpAddress);

                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Unauthorized: Invalid or missing internal API key"
                });
                return;
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method to register InternalApiKeyMiddleware
/// </summary>
public static class InternalApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UseInternalApiKeyProtection(this IApplicationBuilder app)
    {
        return app.UseMiddleware<InternalApiKeyMiddleware>();
    }
}
