using _360Retail.Services.Identity.Application.DTOs;
using _360Retail.Services.Identity.Application.Interfaces;
using _360Retail.Services.Identity.API.Hubs;
using _360Retail.Services.Identity.Domain.Entities;
using _360Retail.Services.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace _360Retail.Services.Identity.API.Services;

public class NotificationService : INotificationService
{
    private readonly IdentityDbContext _db;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IdentityDbContext db,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto)
    {
        var notification = new Notification
        {
            UserId = dto.UserId,
            StoreId = dto.StoreId,
            Title = dto.Title,
            Message = dto.Message,
            Type = dto.Type,
            Link = dto.Link,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        var result = MapToDto(notification);

        // Push real-time via SignalR
        try
        {
            await _hubContext.Clients
                .Group($"user_{dto.UserId}")
                .SendAsync("ReceiveNotification", result);

            _logger.LogInformation(
                "Notification pushed to user {UserId}: {Title}",
                dto.UserId, dto.Title);
        }
        catch (Exception ex)
        {
            // Don't fail if SignalR push fails (user may be offline)
            _logger.LogWarning(ex,
                "Failed to push notification to user {UserId}", dto.UserId);
        }

        return result;
    }

    public async Task<List<NotificationDto>> GetByUserAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => MapToDto(n))
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            throw new Exception("Notification không tồn tại");

        notification.IsRead = true;
        await _db.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    private static NotificationDto MapToDto(Notification n) => new(
        n.Id,
        n.Title,
        n.Message,
        n.Type,
        n.Link,
        n.IsRead,
        n.CreatedAt
    );
}
