using _360Retail.Services.Identity.Application.DTOs.SuperAdmin;
using _360Retail.Services.Identity.Application.DTOs.SuperAdmin.Tracking;

namespace _360Retail.Services.Identity.Application.Interfaces.SuperAdmin;

public interface ISuperAdminUserService
{
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(CreateUserDto dto);
    Task UpdateAsync(Guid id, UpdateUserDto dto);
    Task DeleteAsync(Guid id);
    
    // Tracking & Stats
    Task<List<DailyRegistrationStatDto>> GetDailyRegistrationStatsAsync(DateTime from, DateTime to);
    Task<List<FunnelStatDto>> GetFunnelStatsAsync(DateTime from, DateTime to);
}
