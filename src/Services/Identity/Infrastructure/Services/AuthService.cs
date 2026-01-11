using _360Retail.Services.Identity.Application.DTOs;
using _360Retail.Services.Identity.Application.Interfaces;
using _360Retail.Services.Identity.Domain.Entities;
using _360Retail.Services.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
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

    public AuthService(
        IdentityDbContext db,
        IConfiguration config,
        IEmailService emailService,
        IPasswordHasher<AppUser> passwordHasher
    )
    {
        _db = db;
        _config = config;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
    }

    // LOGIN
    public async Task<AuthResultDto> LoginAsync(LoginDto dto)
    {
        var user = await _db.AppUsers
            .Include(u => u.StoreAccesses)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u =>
                u.Email == dto.Email &&
                u.IsActivated &&
                u.Status == "Active"
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


    // REGISTER (USER ONLY – NO STORE)
    public async Task RegisterAsync(RegisterUserDto dto)
    {
        if (await _db.AppUsers.AnyAsync(u => u.Email == dto.Email))
            throw new Exception("Email already exists");

        var user = new AppUser
        {
            Email = dto.Email,
            UserName = dto.Email,
            PasswordHash = HashPassword(dto.Password),
            Status = "Active",
            IsActivated = true
        };
        var ownerRole = await _db.AppRoles
        .FirstAsync(r => r.RoleName == "StoreOwner");

        user.Roles.Add(ownerRole);

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
            // Map to standard Role claim so [Authorize(Roles="...")] works
            claims.Add(new Claim(ClaimTypes.Role, defaultAccess.RoleInStore));
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
}
