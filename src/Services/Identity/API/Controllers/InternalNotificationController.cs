using _360Retail.Services.Identity.Application.DTOs;
using _360Retail.Services.Identity.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace _360Retail.Services.Identity.API.Controllers;

/// <summary>
/// Internal API cho các service khác gọi tạo notification (dùng API key)
/// </summary>
[ApiController]
[Route("api/internal/notifications")]
public class InternalNotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InternalNotificationController> _logger;

    public InternalNotificationController(
        INotificationService notificationService,
        IConfiguration configuration,
        ILogger<InternalNotificationController> logger)
    {
        _notificationService = notificationService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Tạo notification + push real-time via SignalR
    /// Gọi từ Sales/HR/SaaS/CRM API với header X-Internal-Key
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto dto)
    {
        // Validate internal API key
        var expectedKey = _configuration["InternalApi:Key"] ?? "360retail-internal-secret-key";
        var providedKey = Request.Headers["X-Internal-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
        {
            _logger.LogWarning("Invalid internal API key for notification creation");
            return Unauthorized(new { success = false, message = "Invalid API key" });
        }

        var result = await _notificationService.CreateAsync(dto);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Tạo notification cho nhiều users cùng lúc (ví dụ: cảnh báo tồn kho cho Owner + Manager)
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> CreateBulkNotifications([FromBody] List<CreateNotificationDto> dtos)
    {
        var expectedKey = _configuration["InternalApi:Key"] ?? "360retail-internal-secret-key";
        var providedKey = Request.Headers["X-Internal-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
            return Unauthorized(new { success = false, message = "Invalid API key" });

        var results = new List<NotificationDto>();
        foreach (var dto in dtos)
        {
            var result = await _notificationService.CreateAsync(dto);
            results.Add(result);
        }

        return Ok(new { success = true, data = results, count = results.Count });
    }
}
