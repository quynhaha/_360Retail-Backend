using _360Retail.Services.HR.Application.Interfaces;
using _360Retail.Shared.Email;
using Microsoft.Extensions.Logging;

namespace _360Retail.Services.HR.Infrastructure.Services.Email;

public class ResendEmailService : IEmailService
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(IEmailSender emailSender, ILogger<ResendEmailService> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendTaskAssignmentEmailAsync(
        string toEmail,
        string assigneeName,
        string taskTitle,
        string? priority,
        string? description,
        DateTime? deadline)
    {
        var html = EmailTemplateService.TaskAssignment(
            assigneeName,
            taskTitle,
            priority ?? "Medium",
            description,
            deadline
        );

        await _emailSender.SendAsync(toEmail, $"[360Retail] Task mới: {taskTitle}", html);
        _logger.LogInformation("Task assignment email sent to {Email} for task: {TaskTitle}", toEmail, taskTitle);
    }
}
