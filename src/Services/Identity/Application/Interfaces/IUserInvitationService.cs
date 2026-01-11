using _360Retail.Services.Identity.Application.DTOs;

namespace _360Retail.Services.Identity.Application.Interfaces;

public interface IUserInvitationService
{
    Task InviteUserAsync(InviteUserDto dto);
}
