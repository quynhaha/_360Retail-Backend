using _360Retail.Services.HR.Application.DTOs;
using _360Retail.Services.HR.Application.Interfaces;
using _360Retail.Services.HR.Domain.Entities;
using _360Retail.Services.HR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace _360Retail.Services.HR.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly HrDbContext _db;
    private readonly HttpClient _identityClient;

    public EmployeeService(HrDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _identityClient = httpClientFactory.CreateClient("IdentityService");
    }

    /// <summary>
    /// Internal: Identity calls this to create employee after invite
    /// </summary>
    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto)
    {
        // Check if employee already exists
        var existing = await _db.Employees
            .FirstOrDefaultAsync(e => e.AppUserId == dto.AppUserId && e.StoreId == dto.StoreId);
        
        if (existing != null)
            throw new Exception("Employee already exists for this user and store");

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            AppUserId = dto.AppUserId,
            StoreId = dto.StoreId,
            FullName = dto.Email,  // Use email as initial name
            Position = dto.Role,   // Staff or Manager
            JoinDate = DateTime.UtcNow,
            Status = "Active"
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        return new EmployeeDto
        {
            Id = employee.Id,
            AppUserId = employee.AppUserId,
            StoreId = employee.StoreId,
            FullName = employee.FullName,
            Position = employee.Position,
            Email = dto.Email,
            JoinDate = employee.JoinDate,
            Status = employee.Status
        };
    }

    /// <summary>
    /// Get employee profile by AppUserId (for /me endpoint)
    /// </summary>
    public async Task<EmployeeDto?> GetByAppUserIdAsync(Guid appUserId, Guid storeId)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.AppUserId == appUserId && e.StoreId == storeId);

        if (employee == null)
            return null;

        // Get user info from Identity
        var userInfo = await GetUserInfoFromIdentity(appUserId);

        return new EmployeeDto
        {
            Id = employee.Id,
            AppUserId = employee.AppUserId,
            StoreId = employee.StoreId,
            FullName = employee.FullName,
            Position = employee.Position,
            UserName = userInfo?.UserName,
            Email = userInfo?.Email,
            PhoneNumber = userInfo?.PhoneNumber,
            BaseSalary = employee.BaseSalary,
            JoinDate = employee.JoinDate,
            Status = employee.Status,
            AvatarUrl = employee.AvatarUrl
        };
    }

    /// <summary>
    /// Update employee profile (partial update)
    /// </summary>
    public async Task<bool> UpdateProfileAsync(Guid appUserId, Guid storeId, UpdateEmployeeProfileDto dto)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.AppUserId == appUserId && e.StoreId == storeId);

        if (employee == null)
            return false;

        // Update HR fields (partial update)
        if (!string.IsNullOrWhiteSpace(dto.FullName))
            employee.FullName = dto.FullName;

        await _db.SaveChangesAsync();

        // Update Identity fields if provided
        if (!string.IsNullOrWhiteSpace(dto.UserName) || !string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            await UpdateUserInIdentity(appUserId, dto.UserName, dto.PhoneNumber);
        }

        return true;
    }

    /// <summary>
    /// Update employee avatar URL
    /// </summary>
    public async Task<bool> UpdateAvatarAsync(Guid appUserId, Guid storeId, string avatarUrl)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.AppUserId == appUserId && e.StoreId == storeId);

        if (employee == null)
            return false;

        employee.AvatarUrl = avatarUrl;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<UserInfoResponse?> GetUserInfoFromIdentity(Guid appUserId)
    {
        try
        {
            var response = await _identityClient.GetAsync($"/identity/internal/users/{appUserId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserInfoResponse>();
            }
        }
        catch
        {
            // Log error, return null if Identity service unavailable
        }
        return null;
    }

    private async Task UpdateUserInIdentity(Guid appUserId, string? userName, string? phoneNumber)
    {
        var payload = new { userName, phoneNumber };
        try
        {
            await _identityClient.PutAsJsonAsync($"/identity/internal/users/{appUserId}", payload);
        }
        catch
        {
            // Log error, but don't fail the whole operation
            throw new Exception("Failed to update user info in Identity service");
        }
    }

    private class UserInfoResponse
    {
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
