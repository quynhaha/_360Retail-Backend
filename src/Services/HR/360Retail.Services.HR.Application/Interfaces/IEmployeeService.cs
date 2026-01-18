using _360Retail.Services.HR.Application.DTOs;

namespace _360Retail.Services.HR.Application.Interfaces;

public interface IEmployeeService
{
    /// <summary>
    /// Internal: Identity calls this to create employee after invite
    /// </summary>
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto);
    
    /// <summary>
    /// Get employee profile by AppUserId (for /me endpoint)
    /// </summary>
    Task<EmployeeDto?> GetByAppUserIdAsync(Guid appUserId, Guid storeId);
    
    /// <summary>
    /// Update employee profile (partial update)
    /// </summary>
    Task<bool> UpdateProfileAsync(Guid appUserId, Guid storeId, UpdateEmployeeProfileDto dto);
    
    /// <summary>
    /// Update employee avatar URL
    /// </summary>
    Task<bool> UpdateAvatarAsync(Guid appUserId, Guid storeId, string avatarUrl);
}
