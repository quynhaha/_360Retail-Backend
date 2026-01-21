using _360Retail.Services.Identity.Application.DTOs;
using _360Retail.Services.Identity.Application.Interfaces;
using _360Retail.Services.Identity.Domain.Entities;
using _360Retail.Services.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace _360Retail.Services.Identity.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IdentityDbContext _db;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthService(
        IdentityDbContext db,
        IConfiguration config,
        IEmailService emailService,
        IPasswordHasher<AppUser> passwordHasher,
        IHttpClientFactory httpClientFactory
    )
    {
        _db = db;
        _config = config;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
        _httpClientFactory = httpClientFactory;
    }

    // LOGIN
    public async Task<AuthResultDto> LoginAsync(LoginDto dto)
    {
        // Allow login for Registered, Trial, and Active users
        var validStatuses = new[] { "Registered", "Trial", "Active" };
        
        var user = await _db.AppUsers
            .Include(u => u.StoreAccesses)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u =>
                u.Email == dto.Email &&
                u.IsActivated &&
                validStatuses.Contains(u.Status)
            );

        if (user == null)
            throw new Exception("Invalid email or password");

        var verifyResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            dto.Password
        );

        if (verifyResult == PasswordVerificationResult.Failed)
            throw new Exception("Invalid email or password");

        var token = GenerateJwtToken(user);
        var expireMinutes = GetJwtExpireMinutes();

        return new AuthResultDto(
            token,
            DateTime.UtcNow.AddMinutes(expireMinutes),
            user.MustChangePassword
        );
    }


    // REGISTER (Creates PotentialOwner - no trial yet, no store)
    public async Task RegisterAsync(RegisterUserDto dto)
    {
        if (await _db.AppUsers.AnyAsync(u => u.Email == dto.Email))
            throw new Exception("Email already exists");

        var user = new AppUser
        {
            Email = dto.Email,
            UserName = dto.Email,
            Status = "Registered",  // Not trial yet, waiting for StartTrial
            IsActivated = true,
            MustChangePassword = false
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        // Assign PotentialOwner role (not StoreOwner yet)
        var potentialOwnerRole = await _db.AppRoles
            .FirstOrDefaultAsync(r => r.RoleName == "PotentialOwner");
        
        if (potentialOwnerRole == null)
        {
            // Create role if not exists (should be created by migration)
            potentialOwnerRole = new AppRole { RoleName = "PotentialOwner" };
            _db.AppRoles.Add(potentialOwnerRole);
        }

        user.Roles.Add(potentialOwnerRole);

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();
    }



    public async Task AssignStoreAsync(Guid userId, AssignStoreDto dto)
    {
        // 1. Check if access already exists
        var exists = await _db.UserStoreAccess.AnyAsync(x => x.UserId == userId && x.StoreId == dto.StoreId);
        if (exists) return; // Already linked

        // 2. Add New Access
        _db.UserStoreAccess.Add(new _360Retail.Services.Identity.Domain.Entities.UserStoreAccess
        {
            UserId = userId,
            StoreId = dto.StoreId,
            RoleInStore = dto.RoleInStore,
            IsDefault = dto.IsDefault,
            AssignedAt = DateTime.UtcNow
        });

        // 3. If default, unset other defaults
        if (dto.IsDefault)
        {
             var others = await _db.UserStoreAccess
                .Where(x => x.UserId == userId && x.StoreId != dto.StoreId)
                .ToListAsync();
             foreach (var item in others) item.IsDefault = false;
        }

        await _db.SaveChangesAsync();
    }

    // REFRESH ACCESS (NEW)
    public async Task<AuthResultDto> RefreshAccessAsync(Guid userId, Guid? storeId)
    {
        var user = await _db.AppUsers
            .Include(u => u.StoreAccesses)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActivated == true);

        if (user == null)
            throw new Exception("User not found or inactive");

        // If user wants to switch store
        if (storeId.HasValue)
        {
            // Verify access
            var targetAccess = user.StoreAccesses.FirstOrDefault(x => x.StoreId == storeId.Value);
            if (targetAccess == null)
                throw new Exception("Access denied to this store");

            // Update IsDefault in DB for next logins
            foreach (var access in user.StoreAccesses) access.IsDefault = false;
            targetAccess.IsDefault = true;
            await _db.SaveChangesAsync();
        }

        // Generate new token
        var newLinkToken = GenerateJwtToken(user);
        var expireMinutes = GetJwtExpireMinutes();

        return new AuthResultDto(newLinkToken, DateTime.UtcNow.AddMinutes(expireMinutes), user.MustChangePassword);
    }

    // JWT
    private string GenerateJwtToken(AppUser user)
    {
        var jwtSettings = _config.GetSection("JwtSettings");

        var keyValue = jwtSettings["Key"];
        if (string.IsNullOrWhiteSpace(keyValue))
            throw new Exception("JWT Key is missing");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(keyValue)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("id", user.Id.ToString()), // For consistency with BaseApiController
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("status", user.Status)
        };

        // Add trial_expired claim for TrialCheckFilter middleware
        if (user.Status == "Trial")
        {
            var isExpired = user.TrialEndDate.HasValue && user.TrialEndDate.Value <= DateTime.UtcNow;
            claims.Add(new Claim("trial_expired", isExpired.ToString().ToLower()));
            
            if (user.TrialEndDate.HasValue)
            {
                claims.Add(new Claim("trial_end_date", user.TrialEndDate.Value.ToString("o")));
                claims.Add(new Claim("trial_days_remaining", user.TrialDaysRemaining.ToString()));
            }
        }

        // System role
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
        }

        // Store access (multi-store SAAS)
        var defaultAccess = user.StoreAccesses.FirstOrDefault(x => x.IsDefault);

        if (defaultAccess != null)
        {
            claims.Add(new Claim("store_id", defaultAccess.StoreId.ToString()));
            claims.Add(new Claim("store_role", defaultAccess.RoleInStore));
            // Map RoleInStore to Role claim: "Owner" -> "StoreOwner" for controller compatibility
            var mappedRole = defaultAccess.RoleInStore == "Owner" 
                ? "StoreOwner" 
                : defaultAccess.RoleInStore;
            claims.Add(new Claim(ClaimTypes.Role, mappedRole));
        }

        var expireMinutes = GetJwtExpireMinutes();

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetJwtExpireMinutes()
    {
        var value = _config["JwtSettings:ExpireMinutes"];
        if (!int.TryParse(value, out var minutes))
            minutes = 120; // default safe

        return minutes;
    }

    // PASSWORD
    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(
            sha.ComputeHash(Encoding.UTF8.GetBytes(password))
        );
    }

    private static bool VerifyPassword(string password, string? hash)
    {
        if (string.IsNullOrEmpty(hash)) return false;
        return HashPassword(password) == hash;
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest dto)
    {
        var user = await _db.AppUsers
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActivated);

        if (user == null)
            throw new Exception("User not found");

        //Verify current password
        var verifyResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            dto.CurrentPassword
        );

        if (verifyResult == PasswordVerificationResult.Failed)
            throw new Exception("Current password is incorrect");

        //Validate new password
        if (dto.NewPassword != dto.ConfirmNewPassword)
            throw new Exception("Password confirmation does not match");

        // (Optional) tránh đổi lại mật khẩu cũ
        if (_passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                dto.NewPassword
            ) == PasswordVerificationResult.Success)
        {
            throw new Exception("New password must be different from old password");
        }

        //Update password + clear flag
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        user.MustChangePassword = false;

        await _db.SaveChangesAsync();
    }

    // START TRIAL - Creates trial store and sets trial period
    public async Task<StartTrialResultDto> StartTrialAsync(Guid userId, string? storeName)
    {
        var user = await _db.AppUsers
            .Include(u => u.StoreAccesses)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new Exception("User not found");

        // Check if user already has trial or active subscription
        if (user.TrialStartDate.HasValue)
            throw new Exception("Trial already started");

        if (user.StoreAccesses.Any())
            throw new Exception("User already has store access");

        // Set trial period (7 days)
        user.TrialStartDate = DateTime.UtcNow;
        user.TrialEndDate = DateTime.UtcNow.AddDays(7);
        user.Status = "Trial";

        // Call SaaS service to create trial store
        var saasClient = _httpClientFactory.CreateClient("SaasService");
        var createStoreRequest = new
        {
            StoreName = storeName ?? $"Store của {user.Email}",
            IsTrial = true
        };

        var response = await saasClient.PostAsJsonAsync("/api/stores/trial", createStoreRequest);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create trial store: {error}");
        }

        var storeResult = await response.Content.ReadFromJsonAsync<CreateTrialStoreResponse>();
        
        if (storeResult == null)
            throw new Exception("Invalid response from SaaS service");

        // Link user to store
        _db.UserStoreAccess.Add(new Domain.Entities.UserStoreAccess
        {
            UserId = userId,
            StoreId = storeResult.StoreId,
            RoleInStore = "Owner",
            IsDefault = true,
            AssignedAt = DateTime.UtcNow
        });

        // Upgrade user role: PotentialOwner -> StoreOwner
        // This ensures they have proper system-level permissions
        var storeOwnerRole = await _db.AppRoles.FirstOrDefaultAsync(r => r.RoleName == "StoreOwner");
        if (storeOwnerRole == null)
        {
            storeOwnerRole = new AppRole { RoleName = "StoreOwner" };
            _db.AppRoles.Add(storeOwnerRole);
        }

        // Add StoreOwner role if not already present
        // Need to reload roles since they weren't included in initial query
        await _db.Entry(user).Collection(u => u.Roles).LoadAsync();
        
        if (!user.Roles.Any(r => r.RoleName == "StoreOwner"))
        {
            user.Roles.Add(storeOwnerRole);
        }

        // Remove PotentialOwner role (optional, keeps clean role assignment)
        var potentialRole = user.Roles.FirstOrDefault(r => r.RoleName == "PotentialOwner");
        if (potentialRole != null)
        {
            user.Roles.Remove(potentialRole);
        }

        await _db.SaveChangesAsync();

        return new StartTrialResultDto(
            storeResult.StoreId,
            storeResult.StoreName,
            user.TrialEndDate.Value,
            7
        );
    }

    // Helper class for SaaS response
    private record CreateTrialStoreResponse(Guid StoreId, string StoreName);

    // GET SUBSCRIPTION STATUS
    public async Task<SubscriptionStatusDto> GetSubscriptionStatusAsync(Guid userId)
    {
        var user = await _db.AppUsers
            .Include(u => u.StoreAccesses)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new Exception("User not found");

        var defaultStore = user.StoreAccesses.FirstOrDefault(x => x.IsDefault);
        
        string status = user.Status;
        int? daysRemaining = null;

        if (user.Status == "Trial")
        {
            if (user.IsTrialActive)
            {
                daysRemaining = user.TrialDaysRemaining;
            }
            else
            {
                status = "Expired";
                daysRemaining = 0;
            }
        }

        return new SubscriptionStatusDto(
            status,
            defaultStore != null,
            defaultStore?.StoreId,
            user.TrialStartDate,
            user.TrialEndDate,
            daysRemaining,
            user.Status == "Trial" ? "Trial" : null
        );
    }
}

