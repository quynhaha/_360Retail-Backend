using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace _360Retail.Shared.Email;

public class EmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(HttpClient httpClient, IConfiguration config, ILogger<EmailSender> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string htmlBody)
        => SendAsync(new[] { to }, subject, htmlBody);

    public async Task SendAsync(IEnumerable<string> to, string subject, string htmlBody)
    {
        var apiKey = _config["Resend:ApiKey"];
        var fromEmail = _config["Resend:FromEmail"] ?? "noreply@360retail.app";

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Resend API key not configured. Email not sent to {Recipients}", string.Join(", ", to));
            return;
        }

        var request = new
        {
            from = fromEmail,
            to = to.ToArray(),
            subject,
            html = htmlBody
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
                _logger.LogError("Failed to send email to {Recipients}: {StatusCode} - {Error}",
                    string.Join(", ", to), response.StatusCode, body);
            }
            else
            {
                _logger.LogInformation("Email sent successfully to {Recipients}: {Subject}",
                    string.Join(", ", to), subject);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception when sending email to {Recipients}", string.Join(", ", to));
            // Don't throw - email failure shouldn't fail business logic
        }
    }
}
