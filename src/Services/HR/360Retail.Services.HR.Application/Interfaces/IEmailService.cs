namespace _360Retail.Services.HR.Application.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Send email to assignee when a new task is created
    /// </summary>
    Task SendTaskAssignmentEmailAsync(
        string toEmail, 
        string assigneeName,
        string taskTitle,
        string? priority,
        string? description,
        DateTime? deadline);
}
