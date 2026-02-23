namespace _360Retail.Shared.Common.Models;

/// <summary>
/// Standard API response wrapper
/// </summary>
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };
}

/// <summary>
/// API error response with optional error code and validation errors
/// </summary>
public record ApiErrorResponse
{
    public bool Success { get; init; } = false;
    public string Message { get; init; } = null!;
    public string? Code { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }

    public static ApiErrorResponse FromException(string message, string? code = null) =>
        new() { Message = message, Code = code };

    public static ApiErrorResponse FromValidation(IDictionary<string, string[]> errors) =>
        new() { Message = "Validation failed", Code = "VALIDATION_FAILED", Errors = errors };
}
