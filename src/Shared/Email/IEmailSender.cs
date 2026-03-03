namespace _360Retail.Shared.Email;

public interface IEmailSender
{
    /// <summary>
    /// Send an email using Resend API
    /// </summary>
    Task SendAsync(string to, string subject, string htmlBody);
    
    /// <summary>
    /// Send an email to multiple recipients
    /// </summary>
    Task SendAsync(IEnumerable<string> to, string subject, string htmlBody);
}
