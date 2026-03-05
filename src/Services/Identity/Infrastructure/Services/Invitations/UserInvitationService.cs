using _360Retail.Services.Identity.Application.DTOs;
using _360Retail.Services.Identity.Application.Interfaces;
using _360Retail.Services.Identity.Domain.Entities;
using _360Retail.Services.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<UserInvitationService> _logger;

    public UserInvitationService(
        IdentityDbContext db,
        IEmailService emailService,
        IPasswordHasher<AppUser> passwordHasher,
        IHttpClientFactory httpClientFactory,
        ILogger<UserInvitationService> logger)
    {
        _db = db;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
        _hrClient = httpClientFactory.CreateClient("HrService");
        _logger = logger;
    }

    public async Task InviteUserAsync(InviteUserDto dto)
    {
        if (_db.AppUsers.Any(u => u.Email == dto.Email))
            throw new Exception("Email đã tồn tại");

        var role = _db.AppRoles.FirstOrDefault(r => r.RoleName == dto.Role);
        if (role == null)
            throw new Exception($"Không tìm thấy vai trò '{dto.Role}'");

        // Check max_employees limit from store's subscription plan
        var currentStaffCount = await _db.UserStoreAccess
            .CountAsync(x => x.StoreId == dto.StoreId && x.RoleInStore != "Owner");

        var maxEmployees = await GetMaxEmployeesForStoreAsync(dto.StoreId);
        if (maxEmployees.HasValue && currentStaffCount >= maxEmployees.Value)
        {
            throw new Exception(
                $"Đã đạt giới hạn {maxEmployees.Value} nhân viên của gói hiện tại. " +
                $"Vui lòng nâng cấp gói để mời thêm nhân viên.");
        }

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

        // Get store name for branded email (cross-schema query)
        var storeName = await _db.Database
            .SqlQueryRaw<string>(@"SELECT store_name AS ""Value"" FROM saas.stores WHERE id = {0}", dto.StoreId)
            .FirstOrDefaultAsync() ?? "360Retail Store";
        var employeeName = user.UserName ?? user.Email;

        await _emailService.SendStaffInviteEmailAsync(
            user.Email,
            employeeName,
            storeName,
            dto.Role,
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
                _logger.LogError("HR Sync Error - Status: {StatusCode}, Error: {Error}", response.StatusCode, content);
            }
            else 
            {
                _logger.LogInformation("HR Sync Success - Employee created for AppUserId: {AppUserId}", appUserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HR service unavailable during employee sync for AppUserId: {AppUserId}", appUserId);
        }
    }

    private static string GenerateTempPassword()
    {
        return $"Tmp@{Random.Shared.Next(100000, 999999)}";
    }

    /// <summary>
    /// Query Saas DB (cross-schema) for max_employees from store's active plan
    /// </summary>
    private async Task<int?> GetMaxEmployeesForStoreAsync(Guid storeId)
    {
        try
        {
            // Cross-schema query: identity service → saas schema
            var result = await _db.Database
                .SqlQueryRaw<string>(@"
                    SELECT sp.features::text AS ""Value""
                    FROM saas.subscriptions s
                    JOIN saas.service_plans sp ON s.plan_id = sp.id
                    WHERE s.store_id = {0}
                      AND (s.status = 'Active' OR s.status = 'Trial')
                      AND (s.end_date IS NULL OR s.end_date > NOW())
                    ORDER BY s.end_date DESC
                    LIMIT 1", storeId)
                .FirstOrDefaultAsync();

            if (result == null) return null;

            var features = System.Text.Json.JsonSerializer.Deserialize<
                Dictionary<string, System.Text.Json.JsonElement>>(result);

            if (features != null && features.TryGetValue("max_employees", out var maxEmp))
            {
                return maxEmp.GetInt32();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check max_employees for store {StoreId}", storeId);
        }

        return null; // No limit if query fails (fail-open)
    }
}
