using Microsoft.Extensions.DependencyInjection;

namespace _360Retail.Shared.Email;

public static class EmailServiceExtensions
{
    /// <summary>
    /// Register shared email services (EmailSender + EmailTemplateService).
    /// Call this in Program.cs of any service that needs to send emails.
    /// </summary>
    public static IServiceCollection AddSharedEmailServices(this IServiceCollection services)
    {
        services.AddHttpClient<IEmailSender, EmailSender>();
        return services;
    }
}
