using _360Retail.Services.Identity.Application.Interfaces;
using _360Retail.Shared.Email;
using Microsoft.Extensions.Logging;

namespace _360Retail.Services.Identity.Infrastructure.Services.Email;

public class ResendEmailService : IEmailService
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(IEmailSender emailSender, ILogger<ResendEmailService> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendStaffInviteEmailAsync(
        string toEmail,
        string employeeName,
        string storeName,
        string role,
        string tempPassword)
    {
        var html = EmailTemplateService.StaffInvite(employeeName, storeName, role, tempPassword);
        await _emailSender.SendAsync(toEmail, $"[360Retail] Chào mừng bạn đến {storeName}", html);
        _logger.LogInformation("Staff invite email sent to {Email} for store {Store}", toEmail, storeName);
    }

    public async Task SendForgotPasswordEmailAsync(
        string toEmail,
        string userName,
        string resetCode,
        int expiryMinutes = 15)
    {
        var html = EmailTemplateService.ForgotPassword(userName, resetCode, expiryMinutes);
        await _emailSender.SendAsync(toEmail, "[360Retail] Đặt lại mật khẩu", html);
        _logger.LogInformation("Forgot password email sent to {Email}", toEmail);
    }
}
