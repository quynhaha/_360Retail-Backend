using System.Text.Json;
using _360Retail.Services.CRM.Application.Interfaces;

namespace _360Retail.Services.CRM.API.Middleware;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IIdempotencyRepository repo)
    {
        if (context.Request.Method != "POST" && context.Request.Method != "PATCH")
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var key))
        {
            await _next(context);
            return;
        }

        if (await repo.ExistsAsync(key))
        {
            context.Response.StatusCode = 409; // Or return cached response
            await context.Response.WriteAsJsonAsync(new { error = "Duplicate request" });
            return;
        }

        // Capture response (advanced implementation would wrap stream)
        await _next(context);

        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
             await repo.AddAsync(key, context.Response.StatusCode, "", TimeSpan.FromHours(24));
        }
    }
}
