using _360Retail.Services.Identity.Application.DTOs;
using _360Retail.Services.Identity.Application.Interfaces;
using _360Retail.Services.Identity.Domain.Entities;
using _360Retail.Services.Identity.Infrastructure.Persistence;
using _360Retail.Shared.Common.Exceptions;
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
        
        // First check if user exists but not activated (email not verified)
        var unverifiedUser = await _db.AppUsers
            .FirstOrDefaultAsync(u => u.Email == dto.Email && !u.IsActivated);
        
        if (unverifiedUser != null)
            throw new UnauthorizedAccessException("Vui lòng xác nhận email trước khi đăng nhập. Kiểm tra hộp thư của bạn.");

        var user = await _db.AppUsers
            .Include(u => u.StoreAccesses)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u =>
                u.Email == dto.Email &&
                u.IsActivated &&
                validStatuses.Contains(u.Status)
            );

        if (user == null)
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không chính xác");

        // Check if user has a password (OAuth-only users don't have one)
        if (string.IsNullOrEmpty(user.PasswordHash))
            throw new UnauthorizedAccessException(
                $"Tài khoản này được đăng ký qua {user.AuthProvider}. Vui lòng đăng nhập bằng {user.AuthProvider}.");

        var verifyResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            dto.Password
        );

        if (verifyResult == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không chính xác");

        var token = await GenerateJwtTokenAsync(user);
        var expireMinutes = GetJwtExpireMinutes();

        return new AuthResultDto(
            token,
            DateTime.UtcNow.AddMinutes(expireMinutes),
            user.MustChangePassword
        );
    }


    // REGISTER (Creates PotentialOwner - no trial yet, no store)
    // Now requires email verification via OTP before activation
    public async Task RegisterAsync(RegisterUserDto dto)
    {
        // Block if ANY activated user with this email exists (Local, Google, Facebook)
        var existingActivated = await _db.AppUsers
            .FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActivated);
        
        if (existingActivated != null)
        {
            if (existingActivated.AuthProvider != "Local")
                throw new BusinessException(
                    $"Email này đã được đăng ký qua {existingActivated.AuthProvider}. Vui lòng đăng nhập bằng {existingActivated.AuthProvider}.",
                    "DUPLICATE_EMAIL", 409);
            else
                throw BusinessException.Duplicate("Email", dto.Email);
        }

        // Remove any unverified registration with same email (allow re-register)
        var existingUnverified = await _db.AppUsers
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == dto.Email && !u.IsActivated);
        if (existingUnverified != null)
        {
            // Clear roles first to avoid FK constraint on user_roles join table
            existingUnverified.Roles.Clear();
            _db.AppUsers.Remove(existingUnverified);
            await _db.SaveChangesAsync();
        }

        // Generate 6-digit OTP
        var otpCode = Random.Shared.Next(100000, 999999).ToString();

        var user = new AppUser
        {
            Email = dto.Email,
            UserName = dto.FullName ?? dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Status = "Registered",
            IsActivated = false,  // Must verify email first
            MustChangePassword = false,
            EmailVerificationCode = otpCode,
            EmailVerificationExpiry = DateTime.UtcNow.AddMinutes(10)
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        // Assign PotentialOwner role (not StoreOwner yet)
        var potentialOwnerRole = await _db.AppRoles
            .FirstOrDefaultAsync(r => r.RoleName == "PotentialOwner");
        
        if (potentialOwnerRole == null)
        {
            potentialOwnerRole = new AppRole { RoleName = "PotentialOwner" };
            _db.AppRoles.Add(potentialOwnerRole);
        }

        user.Roles.Add(potentialOwnerRole);

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        // Send verification email via Resend
        await _emailService.SendVerificationEmailAsync(
            user.Email,
            user.UserName ?? user.Email,
            otpCode,
            10
        );
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
            throw new BusinessException("Không tìm thấy người dùng hoặc tài khoản không hoạt động", "NOT_FOUND", 404);

        // If user wants to switch store
        if (storeId.HasValue)
        {
            // Verify access
            var targetAccess = user.StoreAccesses.FirstOrDefault(x => x.StoreId == storeId.Value);
            if (targetAccess == null)
                throw BusinessException.Forbidden("Bạn không có quyền truy cập cửa hàng này");

            // Check if store is active (paid) by calling SaaS service
            var isStoreActive = await CheckStoreActiveAsync(storeId.Value);
            if (!isStoreActive)
                throw BusinessException.Forbidden("Cửa hàng chưa được kích hoạt. Vui lòng hoàn tất thanh toán.");

            // Update IsDefault in DB for next logins
            foreach (var access in user.StoreAccesses) access.IsDefault = false;
            targetAccess.IsDefault = true;
            await _db.SaveChangesAsync();
        }

        // Generate new token
        var newLinkToken = await GenerateJwtTokenAsync(user);
        var expireMinutes = GetJwtExpireMinutes();

        return new AuthResultDto(newLinkToken, DateTime.UtcNow.AddMinutes(expireMinutes), user.MustChangePassword);
    }

    // JWT
    private async Task<string> GenerateJwtTokenAsync(AppUser user)
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
            new Claim("full_name", user.UserName ?? user.Email),
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

            // Check subscription expiry for Active users (Paid users)
            if (user.Status == "Active")
            {
                var subscriptionExpired = await CheckSubscriptionExpiredAsync(defaultAccess.StoreId);
                claims.Add(new Claim("subscription_expired", subscriptionExpired.ToString().ToLower()));
            }
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

    /// <summary>
    /// Check if store's subscription has expired by calling SaaS service
    /// </summary>
    private async Task<bool> CheckSubscriptionExpiredAsync(Guid storeId)
    {
        try
        {
            var saasClient = _httpClientFactory.CreateClient("SaasService");
            var response = await saasClient.GetAsync($"/api/subscriptions/store/{storeId}/status");
            
            if (!response.IsSuccessStatusCode)
            {
                // If we can't reach SaaS service, assume not expired (fail-open for better UX)
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<SubscriptionStatusResponse>();
            
            if (result == null || result.Status == "NoSubscription")
            {
                // No subscription means expired/not active
                return true;
            }

            // Check if subscription end date has passed
            if (result.EndDate.HasValue && result.EndDate.Value <= DateTime.UtcNow)
            {
                return true;
            }

            return result.Status != "Active" && result.Status != "Trial";
        }
        catch
        {
            // On error, fail-open to avoid blocking users due to service issues
            return false;
        }
    }

    private record SubscriptionStatusResponse(
        Guid? Id,
        string? PlanName,
        decimal? Price,
        DateTime? StartDate,
        DateTime? EndDate,
        string Status,
        int? DaysRemaining
    );

    /// <summary>
    /// Check if store is active (paid) by calling SaaS service
    /// </summary>
    private async Task<bool> CheckStoreActiveAsync(Guid storeId)
    {
        try
        {
            var saasClient = _httpClientFactory.CreateClient("SaasService");
            var response = await saasClient.GetAsync($"/api/stores/{storeId}/active-status");
            
            if (!response.IsSuccessStatusCode)
            {
                // If we can't reach SaaS service, fail-closed for security (block access)
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<StoreActiveResponse>();
            return result?.IsActive ?? false;
        }
        catch
        {
            // On error, fail-closed for security
            return false;
        }
    }

    private record StoreActiveResponse(bool IsActive);

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
            throw BusinessException.NotFound("User", userId);

        //Verify current password
        var verifyResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            dto.CurrentPassword
        );

        if (verifyResult == PasswordVerificationResult.Failed)
            throw BusinessException.ValidationFailed("Mật khẩu hiện tại không chính xác");

        //Validate new password
        if (dto.NewPassword != dto.ConfirmNewPassword)
            throw BusinessException.ValidationFailed("Xác nhận mật khẩu không khớp");

        // (Optional) tránh đổi lại mật khẩu cũ
        if (_passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                dto.NewPassword
            ) == PasswordVerificationResult.Success)
        {
            throw BusinessException.ValidationFailed("Mật khẩu mới phải khác mật khẩu cũ");
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
            throw BusinessException.NotFound("User", userId);

        // Check if user already has trial or active subscription
        if (user.TrialStartDate.HasValue)
            throw BusinessException.ValidationFailed("Bạn đã bắt đầu dùng thử rồi");

        if (user.StoreAccesses.Any())
            throw BusinessException.ValidationFailed("Người dùng đã có quyền truy cập cửa hàng");

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
            throw new Exception($"Tạo cửa hàng dùng thử thất bại: {error}");
        }

        var storeResult = await response.Content.ReadFromJsonAsync<CreateTrialStoreResponse>();
        
        if (storeResult == null)
            throw new Exception("Phản hồi không hợp lệ từ dịch vụ SaaS");

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
            throw BusinessException.NotFound("User", userId);

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

    // EXTERNAL OAUTH LOGIN (Google, Facebook)
    public async Task<ExternalAuthResultDto> ExternalLoginAsync(ExternalLoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Provider) || string.IsNullOrWhiteSpace(dto.IdToken))
            throw BusinessException.ValidationFailed("Provider và IdToken là bắt buộc");

        // Validate token based on provider
        GoogleUserInfo? userInfo = null;
        
        if (dto.Provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
        {
            userInfo = await ValidateGoogleTokenAsync(dto.IdToken);
        }
        else
        {
            throw BusinessException.ValidationFailed($"Provider phải là Google hoặc Facebook");
        }

        if (userInfo == null || string.IsNullOrEmpty(userInfo.Email))
            throw BusinessException.ValidationFailed("Xác thực token thất bại hoặc không lấy được thông tin");

        // Find existing user by external ID first, then by email
        var existingUser = await _db.AppUsers
            .Include(u => u.StoreAccesses)
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => 
                u.AuthProvider == dto.Provider && u.ExternalUserId == userInfo.Sub);

        // If not found by external ID, search by email (for account linking)
        if (existingUser == null)
        {
            existingUser = await _db.AppUsers
                .Include(u => u.StoreAccesses)
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Email == userInfo.Email);
        }

        bool isNewUser = existingUser == null;

        if (existingUser != null)
        {
            // Case 1: Unverified local user → activate + link Google (skip OTP)
            if (!existingUser.IsActivated)
            {
                existingUser.IsActivated = true;
                existingUser.EmailVerificationCode = null;
                existingUser.EmailVerificationExpiry = null;
            }

            // Case 2: Local user → link Google (keep existing password for dual login)
            if (existingUser.AuthProvider == "Local")
            {
                existingUser.AuthProvider = dto.Provider;
                existingUser.ExternalUserId = userInfo.Sub;
                existingUser.ProfilePictureUrl = userInfo.Picture;
            }
            // Case 3: Same provider, update profile picture if changed
            else if (existingUser.ProfilePictureUrl != userInfo.Picture)
            {
                existingUser.ProfilePictureUrl = userInfo.Picture;
            }
            
            await _db.SaveChangesAsync();
        }
        else
        {
            // Create new user from OAuth
            existingUser = new AppUser
            {
                Email = userInfo.Email,
                UserName = userInfo.Name ?? userInfo.Email,
                AuthProvider = dto.Provider,
                ExternalUserId = userInfo.Sub,
                ProfilePictureUrl = userInfo.Picture,
                Status = "Registered",  // Will need to start trial
                IsActivated = true,
                MustChangePassword = false,
                PasswordHash = null  // No password for OAuth users
            };

            // Assign PotentialOwner role
            var potentialOwnerRole = await _db.AppRoles
                .FirstOrDefaultAsync(r => r.RoleName == "PotentialOwner");
            
            if (potentialOwnerRole == null)
            {
                potentialOwnerRole = new AppRole { RoleName = "PotentialOwner" };
                _db.AppRoles.Add(potentialOwnerRole);
            }

            existingUser.Roles.Add(potentialOwnerRole);
            _db.AppUsers.Add(existingUser);
            await _db.SaveChangesAsync();
        }

        // Generate JWT token
        var token = await GenerateJwtTokenAsync(existingUser);
        var expireMinutes = GetJwtExpireMinutes();

        return new ExternalAuthResultDto(
            token,
            DateTime.UtcNow.AddMinutes(expireMinutes),
            isNewUser,
            existingUser.Email,
            existingUser.ProfilePictureUrl
        );
    }

    // Validate Google ID Token
    private async Task<GoogleUserInfo?> ValidateGoogleTokenAsync(string idToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(
                $"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}");
            
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var tokenInfo = System.Text.Json.JsonSerializer.Deserialize<GoogleUserInfo>(content, 
                new System.Text.Json.JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

            // Validate the token is for our app
            // Try environment variable first, then fall back to appsettings
            var expectedClientId = Environment.GetEnvironmentVariable("OAUTH_GOOGLE_CLIENT_ID") 
                ?? _config["OAuth:Google:ClientId"];
            
            if (string.IsNullOrEmpty(expectedClientId))
            {
                throw new Exception("Chưa cấu hình Google OAuth Client ID");
            }
            
            var validClientIds = expectedClientId.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(id => id.Trim());

            if (tokenInfo?.Aud == null || !validClientIds.Contains(tokenInfo.Aud))
            {
                throw BusinessException.ValidationFailed("Token không thuộc ứng dụng này");
            }

            return tokenInfo;
        }
        catch (Exception ex)
        {
            throw new Exception($"Xác thực Google token thất bại: {ex.Message}");
        }
    }

    // Google user info from token validation
    // Note: Google tokeninfo API returns ALL values as strings
    private record GoogleUserInfo(
        string Sub,      // User ID
        string Email,
        string? Email_verified,
        string? Name,
        string? Picture,
        string? Aud,     // Client ID
        string? Iss,     // Issuer
        string? Exp      // Expiration (as string)
    );

    // ==================== FORGOT PASSWORD ====================
    
    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == email && u.IsActivated);
        
        if (user == null)
        {
            // Don't reveal if email exists or not (security)
            return;
        }

        // Generate 6-digit reset code
        var resetCode = Random.Shared.Next(100000, 999999).ToString();
        user.PasswordResetCode = resetCode;
        user.PasswordResetExpiry = DateTime.UtcNow.AddMinutes(15);
        
        await _db.SaveChangesAsync();

        // Send branded email
        await _emailService.SendForgotPasswordEmailAsync(
            user.Email,
            user.UserName ?? user.Email,
            resetCode,
            15
        );
    }

    public async Task ResetPasswordAsync(string email, string resetCode, string newPassword)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == email && u.IsActivated);
        
        if (user == null)
            throw BusinessException.ValidationFailed("Email không hợp lệ");

        if (string.IsNullOrEmpty(user.PasswordResetCode) || user.PasswordResetCode != resetCode)
            throw BusinessException.ValidationFailed("Mã đặt lại không chính xác");

        if (user.PasswordResetExpiry == null || user.PasswordResetExpiry < DateTime.UtcNow)
            throw BusinessException.ValidationFailed("Mã đặt lại đã hết hạn. Vui lòng yêu cầu mã mới");

        // Update password
        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        user.PasswordResetCode = null;
        user.PasswordResetExpiry = null;
        user.MustChangePassword = false;
        
        await _db.SaveChangesAsync();
    }

    // ==================== EMAIL VERIFICATION (OTP) ====================

    public async Task VerifyEmailAsync(string email, string otpCode)
    {
        var user = await _db.AppUsers
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsActivated);

        if (user == null)
            throw BusinessException.ValidationFailed("Email không tồn tại hoặc đã được xác nhận");

        if (string.IsNullOrEmpty(user.EmailVerificationCode) || user.EmailVerificationCode != otpCode)
            throw BusinessException.ValidationFailed("Mã OTP không chính xác");

        if (user.EmailVerificationExpiry == null || user.EmailVerificationExpiry < DateTime.UtcNow)
            throw BusinessException.ValidationFailed("Mã OTP đã hết hạn. Vui lòng yêu cầu gửi lại");

        // Activate the account
        user.IsActivated = true;
        user.EmailVerificationCode = null;
        user.EmailVerificationExpiry = null;

        await _db.SaveChangesAsync();
    }

    public async Task ResendOtpAsync(string email)
    {
        var user = await _db.AppUsers
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsActivated);

        if (user == null)
        {
            // Don't reveal if email exists (security)
            return;
        }

        // Generate new 6-digit OTP
        var otpCode = Random.Shared.Next(100000, 999999).ToString();
        user.EmailVerificationCode = otpCode;
        user.EmailVerificationExpiry = DateTime.UtcNow.AddMinutes(10);

        await _db.SaveChangesAsync();

        // Send verification email
        await _emailService.SendVerificationEmailAsync(
            user.Email,
            user.UserName ?? user.Email,
            otpCode,
            10
        );
    }
}


