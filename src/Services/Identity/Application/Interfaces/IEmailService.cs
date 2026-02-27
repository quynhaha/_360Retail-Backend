namespace _360Retail.Services.Identity.Application.Interfaces;

public interface IEmailService
{
    Task SendStaffInviteEmailAsync(
        string toEmail,
        string employeeName,
        string storeName,
        string role,
        string tempPassword);

    Task SendForgotPasswordEmailAsync(
        string toEmail,
        string userName,
        string resetCode,
        int expiryMinutes = 15);
}
