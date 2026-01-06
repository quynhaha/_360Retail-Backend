namespace _360Retail.Services.Identity.Application.Interfaces;

public interface IEmailService
{
    Task SendActivationEmailAsync(string toEmail, string activationLink);
}
