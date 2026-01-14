using _360Retail.Services.Identity.Application.DTOs;
using _360Retail.Services.Identity.Application.Interfaces;
using _360Retail.Services.Identity.Domain.Entities;
using _360Retail.Services.Identity.Infrastructure.Persistence;
using _360Retail.Services.Identity.Infrastructure.Services.Email;
using Microsoft.AspNetCore.Identity;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using UserStoreAccessEntity =_360Retail.Services.Identity.Domain.Entities.UserStoreAccess;

namespace _360Retail.Services.Identity.Infrastructure.Services.Invitations;

public class UserInvitationService : IUserInvitationService
{
    private readonly IdentityDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public UserInvitationService(
        IdentityDbContext db,
        IEmailService emailService,
        IPasswordHasher<AppUser> passwordHasher)
    {
        _db = db;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
    }

    public async Task InviteUserAsync(InviteUserDto dto)
    {
        if (_db.AppUsers.Any(u => u.Email == dto.Email))
            throw new Exception("Email already exists");

        var role = _db.AppRoles.FirstOrDefault(r => r.RoleName == dto.Role);
        if (role == null)
            throw new Exception($"Role '{dto.Role}' not found");

        var tempPassword = GenerateTempPassword();

        var user = new AppUser
        {
            Email = dto.Email,
            UserName = dto.Email,
            Status = "Active",
            IsActivated = true,
            MustChangePassword = true,
            StoreId = dto.StoreId
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, tempPassword);

        user.Roles.Add(role);

        user.StoreAccesses.Add(new UserStoreAccessEntity
        {
            UserId = user.Id,            
            StoreId = dto.StoreId,
            RoleInStore = dto.Role,       
            IsDefault = true
        });

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        await _emailService.SendTemporaryPasswordEmailAsync(
            user.Email,
            tempPassword
        );
    }

    private static string GenerateTempPassword()
    {
        return $"Tmp@{Random.Shared.Next(100000, 999999)}";
    }
}
