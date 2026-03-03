namespace _360Retail.Services.Identity.Application.DTOs;

public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string Type,
    string? Link,
    bool IsRead,
    DateTime CreatedAt
);

public record CreateNotificationDto(
    Guid UserId,
    Guid? StoreId,
    string Title,
    string Message,
    string Type,
    string? Link
);

public record UnreadCountDto(int Count);
