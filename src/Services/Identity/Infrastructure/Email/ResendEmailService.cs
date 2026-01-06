using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using _360Retail.Services.Identity.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace _360Retail.Services.Identity.Infrastructure.Email;

public class ResendEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public ResendEmailService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task SendActivationEmailAsync(string toEmail, string activationLink)
    {
        var apiKey = _config["Resend:ApiKey"];
        var fromEmail = _config["Resend:FromEmail"];

        var request = new
        {
            from = fromEmail,
            to = new[] { toEmail },
            subject = "Activate your 360Retail account",
            html = $@"
                <h3>Welcome to 360Retail</h3>
                <p>Please click the link below to activate your account:</p>
                <a href='{activationLink}'>Activate Account</a>
                <br/><br/>
                <small>This link will expire in 48 hours.</small>
            "
        };

        var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.resend.com/emails"
        );

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(httpRequest);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"Resend email failed: {body}");
        }
    }
}
