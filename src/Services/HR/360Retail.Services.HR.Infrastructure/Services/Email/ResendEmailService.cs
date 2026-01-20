using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using _360Retail.Services.HR.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace _360Retail.Services.HR.Infrastructure.Services.Email;

public class ResendEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(HttpClient httpClient, IConfiguration config, ILogger<ResendEmailService> logger)
    {
        _httpClient = httpClient;
        _config = config;
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
        var apiKey = _config["Resend:ApiKey"];
        var fromEmail = _config["Resend:FromEmail"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(fromEmail))
        {
            _logger.LogWarning("Resend API key or FromEmail not configured. Email not sent.");
            return;
        }

        var deadlineText = deadline?.ToString("dd/MM/yyyy HH:mm") ?? "Không có deadline";
        var priorityText = priority ?? "Medium";
        var descriptionText = description ?? "Không có mô tả";

        var request = new
        {
            from = fromEmail,
            to = new[] { toEmail },
            subject = $"[360Retail] New Task Assigned: {taskTitle}",
            html = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2563eb;'>📋 New Task Assigned</h2>
                    <p>Hi <strong>{assigneeName}</strong>,</p>
                    <p>You have been assigned a new task:</p>
                    
                    <div style='background: #f3f4f6; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='margin-top: 0; color: #1f2937;'>{taskTitle}</h3>
                        <table style='width: 100%;'>
                            <tr>
                                <td style='padding: 8px 0; color: #6b7280;'>Priority:</td>
                                <td style='padding: 8px 0;'><strong style='color: {GetPriorityColor(priorityText)};'>{priorityText}</strong></td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0; color: #6b7280;'>Deadline:</td>
                                <td style='padding: 8px 0;'><strong>{deadlineText}</strong></td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0; color: #6b7280;' colspan='2'>Description:</td>
                            </tr>
                            <tr>
                                <td colspan='2' style='padding: 8px 0;'>{descriptionText}</td>
                            </tr>
                        </table>
                    </div>
                    
                    <p style='color: #6b7280; font-size: 14px;'>
                        Login to view your tasks: 
                        <a href='https://360retail.app/tasks' style='color: #2563eb;'>https://360retail.app/tasks</a>
                    </p>
                    
                    <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;'/>
                    <p style='color: #9ca3af; font-size: 12px;'>
                        This is an automated email from 360Retail. Please do not reply.
                    </p>
                </div>
            "
        };

        try
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send task assignment email to {Email}: {Error}", toEmail, body);
            }
            else
            {
                _logger.LogInformation("Task assignment email sent to {Email} for task: {TaskTitle}", toEmail, taskTitle);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception when sending task assignment email to {Email}", toEmail);
            // Don't throw - email failure shouldn't fail task creation
        }
    }

    private static string GetPriorityColor(string priority) => priority.ToLower() switch
    {
        "high" => "#dc2626",
        "medium" => "#f59e0b",
        "low" => "#10b981",
        _ => "#6b7280"
    };
}
