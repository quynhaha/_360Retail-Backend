using _360Retail.Services.Identity.Domain.Entities;

namespace _360Retail.Services.Identity.Application.Interfaces;

public interface IActivationService
{
    Task SendActivationAsync(AppUser user);
}
