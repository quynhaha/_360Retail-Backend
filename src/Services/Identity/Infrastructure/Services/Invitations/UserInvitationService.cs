using _360Retail.Services.Identity.Application.DTOs;
using _360Retail.Services.Identity.Application.Interfaces;
using _360Retail.Services.Identity.Domain.Entities;
using _360Retail.Services.Identity.Infrastructure.Persistence;
using _360Retail.Services.Identity.Infrastructure.Services.Email;
using Microsoft.AspNetCore.Identity;
using System;
using System.Data;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using UserStoreAccessEntity =_360Retail.Services.Identity.Domain.Entities.UserStoreAccess;

namespace _360Retail.Services.Identity.Infrastructure.Services.Invitations;

public class UserInvitationService : IUserInvitationService
{
    private readonly IdentityDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly HttpClient _hrClient;

    public UserInvitationService(
        IdentityDbContext db,
        IEmailService emailService,
        IPasswordHasher<AppUser> passwordHasher,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
        _hrClient = httpClientFactory.CreateClient("HrService");
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

        // Call HR Service to create Employee record
        await CreateEmployeeInHrService(user.Id, dto.StoreId, dto.Email, dto.Role);

        await _emailService.SendTemporaryPasswordEmailAsync(
            user.Email,
            tempPassword
        );
    }

    private async Task CreateEmployeeInHrService(Guid appUserId, Guid storeId, string email, string role)
    {
        try
        {
            // Use same casing as CreateEmployeeDto in HR Service
            var payload = new 
            {
                AppUserId = appUserId,
                StoreId = storeId,
                Email = email,
                Role = role
            };

            var response = await _hrClient.PostAsJsonAsync("/api/employees/internal/create", payload);
            
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                // CRITICAL: Log this better so user can see it in docker logs
                Console.WriteLine($"[HR_SYNC_ERROR] Status: {response.StatusCode}, Error: {content}");
            }
            else 
            {
                Console.WriteLine($"[HR_SYNC_SUCCESS] Employee created for AppUserId: {appUserId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HR_SYNC_EXCEPTION] HR service unavailable: {ex.Message}");
        }
    }

    private static string GenerateTempPassword()
    {
        return $"Tmp@{Random.Shared.Next(100000, 999999)}";
    }
}
