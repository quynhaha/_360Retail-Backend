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
    
    #region Manager/Owner APIs
    
    /// <summary>
    /// Get all employees in a store (for Manager/Owner)
    /// </summary>
    Task<List<EmployeeDto>> GetAllByStoreIdAsync(Guid storeId, bool includeInactive = false);
    
    /// <summary>
    /// Get employee by ID (for Manager/Owner)
    /// </summary>
    Task<EmployeeDto?> GetByIdAsync(Guid employeeId, Guid storeId);
    
    /// <summary>
    /// Update employee by Owner (salary, position, status)
    /// </summary>
    Task<bool> UpdateByOwnerAsync(Guid employeeId, Guid storeId, UpdateEmployeeByOwnerDto dto);
    
    #endregion
}
