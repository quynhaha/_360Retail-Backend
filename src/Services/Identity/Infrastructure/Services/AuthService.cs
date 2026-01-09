using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using _360Retail.Services.Identity.Application.DTOs;
using _360Retail.Services.Identity.Application.Interfaces;
using _360Retail.Services.Identity.Domain.Entities;
using _360Retail.Services.Identity.Infrastructure.Persistence;

namespace _360Retail.Services.Identity.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IdentityDbContext _db;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;

    public AuthService(
        IdentityDbContext db,
        IConfiguration config,
        IEmailService emailService
    )
    {
        _db = db;
        _config = config;
        _emailService = emailService;
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

        if (user == null || !VerifyPassword(dto.Password, user.PasswordHash))
            throw new Exception("Invalid email or password");

        var token = GenerateJwtToken(user);

        var expireMinutes = GetJwtExpireMinutes();

        return new AuthResultDto(
            token,
            DateTime.UtcNow.AddMinutes(expireMinutes)
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

    // INVITE STAFF
    public async Task InviteStaffAsync(Guid ownerUserId, Guid storeId, InviteStaffDto dto)
    {
        if (await _db.AppUsers.AnyAsync(u => u.Email == dto.Email))
            throw new Exception("User already exists");

        var activationToken = Guid.NewGuid().ToString("N");

        var user = new AppUser
        {
            Email = dto.Email,
            UserName = dto.Email,
            Status = "Pending",
            IsActivated = false,
            ActivationToken = activationToken,
            ActivationTokenExpiredAt = DateTime.UtcNow.AddDays(2)
        };

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        _db.UserStoreAccess.Add(new UserStoreAccess
        {
            UserId = user.Id,
            StoreId = storeId,
            RoleInStore = dto.RoleInStore,
            IsDefault = true
        });

        await _db.SaveChangesAsync();

        var activationLink =
            $"{_config["Frontend:BaseUrl"]}/activate?token={activationToken}";

        await _emailService.SendActivationEmailAsync(
            user.Email,
            activationLink
        );
    }

    public async Task ActivateAccountAsync(ActivateAccountDto dto)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u =>
            u.ActivationToken == dto.Token &&
            u.ActivationTokenExpiredAt > DateTime.UtcNow
        );

        if (user == null)
            throw new Exception("Invalid or expired activation token");

        user.PasswordHash = HashPassword(dto.Password);
        user.IsActivated = true;
        user.Status = "Active";
        user.ActivationToken = null;
        user.ActivationTokenExpiredAt = null;

        await _db.SaveChangesAsync();
    }

    public async Task AssignStoreAsync(Guid userId, AssignStoreDto dto)
    {
        // 1. Check if access already exists
        var exists = await _db.UserStoreAccess.AnyAsync(x => x.UserId == userId && x.StoreId == dto.StoreId);
        if (exists) return; // Already linked

        // 2. Add New Access
        _db.UserStoreAccess.Add(new UserStoreAccess
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

        return new AuthResultDto(newLinkToken, DateTime.UtcNow.AddMinutes(expireMinutes));
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
}
