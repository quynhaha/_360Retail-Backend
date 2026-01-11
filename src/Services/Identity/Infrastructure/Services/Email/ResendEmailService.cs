using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using _360Retail.Services.Identity.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace _360Retail.Services.Identity.Infrastructure.Services.Email;

public class ResendEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public ResendEmailService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task SendTemporaryPasswordEmailAsync(string toEmail, string tempPassword)
    {
        var apiKey = _config["Resend:ApiKey"];
        var fromEmail = _config["Resend:FromEmail"];

        var request = new
        {
            from = fromEmail,
            to = new[] { toEmail },
            subject = "Your 360Retail temporary password",
            html = $@"
                <h3>Welcome to 360Retail</h3>
                <p>Your temporary password is:</p>
                <h2>{tempPassword}</h2>
                <p>Please login and change your password immediately.</p>
                <p>
                    Login here:
                    <a href='https://360retail.app/login'>https://360retail.app/login</a>
                </p>
                <br/>
                <small>This password can only be used once.</small>
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
