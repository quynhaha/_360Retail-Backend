using System.Text.Json;
using _360Retail.Shared.Common.Exceptions;
using _360Retail.Shared.Common.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace _360Retail.Shared.Common.Middleware;

/// <summary>
/// Global exception handler that formats all exceptions to consistent API responses
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            BusinessException bex => (bex.StatusCode, ApiErrorResponse.FromException(bex.Message, bex.Code)),
            
            ArgumentException aex => (400, ApiErrorResponse.FromException(aex.Message, "INVALID_ARGUMENT")),
            
            UnauthorizedAccessException uex => (401, ApiErrorResponse.FromException(uex.Message, "UNAUTHORIZED")),
            
            KeyNotFoundException knf => (404, ApiErrorResponse.FromException(knf.Message, "NOT_FOUND")),

            // Database transient failures (EF Core/Npgsql connection issues) → 503
            InvalidOperationException iex when iex.Message.Contains("transient failure", StringComparison.OrdinalIgnoreCase)
                => (503, ApiErrorResponse.FromException(
                    "Hệ thống đang tải. Vui lòng thử lại sau vài giây.", "SERVICE_UNAVAILABLE")),

            InvalidOperationException iex => (409, ApiErrorResponse.FromException(iex.Message, "CONFLICT")),
            
            // Generic exceptions - try to infer status code from message
            _ => HandleGenericException(exception)
        };

        // Log at appropriate level based on status code
        // Business logic errors (4xx) are expected — log as Warning
        // Server errors (5xx) are unexpected — log as Error
        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Server error: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Request rejected ({StatusCode}): {Message}", statusCode, exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }

    private (int statusCode, ApiErrorResponse response) HandleGenericException(Exception ex)
    {
        // Check for common business logic patterns in exception message
        var message = ex.Message.ToLower();
        
        if (message.Contains("already exists") || message.Contains("duplicate"))
            return (409, ApiErrorResponse.FromException(ex.Message, "DUPLICATE"));
        
        if (message.Contains("not found"))
            return (404, ApiErrorResponse.FromException(ex.Message, "NOT_FOUND"));
        
        if (message.Contains("invalid") || message.Contains("incorrect"))
            return (400, ApiErrorResponse.FromException(ex.Message, "INVALID_INPUT"));
        
        if (message.Contains("unauthorized") || message.Contains("access denied"))
            return (403, ApiErrorResponse.FromException(ex.Message, "FORBIDDEN"));

        // For truly unknown errors - hide details in production
        var errorMessage = _env.IsDevelopment() 
            ? ex.Message 
            : "An error occurred. Please try again later.";

        return (500, ApiErrorResponse.FromException(errorMessage, "INTERNAL_ERROR"));
    }
}

/// <summary>
/// Extension method to easily add the middleware
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
