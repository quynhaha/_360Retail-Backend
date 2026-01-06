using _360Retail.Services.Identity.Application.DTOs;

namespace _360Retail.Services.Identity.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResultDto> LoginAsync(LoginDto dto);

    Task RegisterAsync(RegisterUserDto dto);

    Task InviteStaffAsync(Guid ownerUserId, Guid storeId, InviteStaffDto dto);

    Task ActivateAccountAsync(ActivateAccountDto dto);
}
