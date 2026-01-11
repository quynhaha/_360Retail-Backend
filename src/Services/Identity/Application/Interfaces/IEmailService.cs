namespace _360Retail.Services.Identity.Application.Interfaces;

public interface IEmailService
{
    Task SendTemporaryPasswordEmailAsync(string toEmail, string tempPassword);
}
