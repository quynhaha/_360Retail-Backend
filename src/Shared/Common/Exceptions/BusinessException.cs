namespace _360Retail.Shared.Common.Exceptions;

/// <summary>
/// Business exception with error code for consistent API responses
/// </summary>
public class BusinessException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    public BusinessException(string message, string code = "BUSINESS_ERROR", int statusCode = 400) 
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    // Common business exceptions
    public static BusinessException NotFound(string entity, object id) =>
        new($"{entity} with ID '{id}' not found", "NOT_FOUND", 404);

    public static BusinessException Duplicate(string field, object value) =>
        new($"{field} '{value}' already exists", "DUPLICATE", 409);

    public static BusinessException InvalidCredentials() =>
        new("Invalid email or password", "INVALID_CREDENTIALS", 401);

    public static BusinessException Unauthorized(string message = "Unauthorized") =>
        new(message, "UNAUTHORIZED", 401);

    public static BusinessException Forbidden(string message = "Access denied") =>
        new(message, "FORBIDDEN", 403);

    public static BusinessException ValidationFailed(string message) =>
        new(message, "VALIDATION_FAILED", 400);
}
