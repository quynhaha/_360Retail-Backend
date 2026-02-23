using _360Retail.Services.Identity.Application.DTOs;

namespace _360Retail.Services.Identity.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResultDto> LoginAsync(LoginDto dto);

    Task RegisterAsync(RegisterUserDto dto);

    Task AssignStoreAsync(Guid userId, AssignStoreDto dto);

    Task<AuthResultDto> RefreshAccessAsync(Guid userId, Guid? storeId);

    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest dto);

    // Trial methods
    Task<StartTrialResultDto> StartTrialAsync(Guid userId, string? storeName);
    
    Task<SubscriptionStatusDto> GetSubscriptionStatusAsync(Guid userId);

    // External OAuth methods
    Task<ExternalAuthResultDto> ExternalLoginAsync(ExternalLoginDto dto);
}

