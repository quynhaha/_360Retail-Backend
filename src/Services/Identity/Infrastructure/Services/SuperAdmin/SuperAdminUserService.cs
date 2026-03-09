using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using _360Retail.Services.Identity.Application.DTOs.SuperAdmin;
using _360Retail.Services.Identity.Application.DTOs.SuperAdmin.Tracking;
using _360Retail.Services.Identity.Application.Interfaces.SuperAdmin;
using _360Retail.Services.Identity.Domain.Entities;
using _360Retail.Services.Identity.Infrastructure.Persistence;
using _360Retail.Services.Identity.Infrastructure.Services.Tracking;

namespace _360Retail.Services.Identity.Infrastructure.Services.SuperAdmin;

public class SuperAdminUserService : ISuperAdminUserService
{
    private readonly IdentityDbContext _db;
    private readonly RedisTrackingService _trackingService;

    public SuperAdminUserService(IdentityDbContext db, RedisTrackingService trackingService)
    {
        _db = db;
        _trackingService = trackingService;
    }

    // GET ALL USERS
    public async Task<List<UserDto>> GetAllAsync()
    {
        return await _db.AppUsers
            .Include(u => u.Roles)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                IsActivated = u.IsActivated,
                Status = u.Status,
                StoreId = u.StoreId,
                Roles = u.Roles.Select(r => r.RoleName).ToList()
            })
            .ToListAsync();
    }

 
    // GET USER BY ID
    public async Task<UserDto> GetByIdAsync(Guid id)
    {
        var user = await _db.AppUsers
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            throw new Exception("Không tìm thấy người dùng");

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            IsActivated = user.IsActivated,
            Status = user.Status,
            StoreId = user.StoreId,
            Roles = user.Roles.Select(r => r.RoleName).ToList()
        };
    }

 
    // CREATE USER — Admin chỉ được tạo PotentialOwner hoặc StoreOwner
    public async Task<Guid> CreateAsync(CreateUserDto dto)
    {
        var allowedRoles = new[] { "PotentialOwner", "StoreOwner" };
        if (!allowedRoles.Contains(dto.RoleName))
            throw new Exception($"Admin chỉ được tạo tài khoản {string.Join("/", allowedRoles)}. Các role khác do StoreOwner tự mời.");

        if (await _db.AppUsers.AnyAsync(u => u.Email == dto.Email))
            throw new Exception("Email đã tồn tại");

        var role = await _db.AppRoles
            .FirstOrDefaultAsync(r => r.RoleName == dto.RoleName);

        if (role == null)
            throw new Exception("Vai trò không hợp lệ");

        var hasher = new PasswordHasher<AppUser>();
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            UserName = dto.Email,
            Status = "Active",
            IsActivated = true,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = hasher.HashPassword(user, dto.Password);

        user.Roles.Add(role);

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        return user.Id;
    }

 
    // UPDATE USER (PARTIAL UPDATE - only update non-null fields)
    public async Task UpdateAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            throw new Exception("Không tìm thấy người dùng");

        // Only update fields that are provided (not null)
        if (dto.IsActivated.HasValue)
            user.IsActivated = dto.IsActivated.Value;
        
        if (dto.Status != null)
            user.Status = dto.Status;

        await _db.SaveChangesAsync();
    }

    // DELETE USER (soft delete — vô hiệu hóa tài khoản)
    public async Task DeleteAsync(Guid id)
    {
        var user = await _db.AppUsers
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            throw new Exception("Không tìm thấy người dùng");

        // ❗ Không cho xoá SuperAdmin
        if (user.Roles.Any(r => r.RoleName == "SuperAdmin"))
            throw new Exception("Không thể xóa tài khoản SuperAdmin");

        // Soft delete: vô hiệu hóa thay vì xóa thật
        user.IsActivated = false;
        user.Status = "Disabled";
        await _db.SaveChangesAsync();
    }

    // STATS & TRACKING
    public async Task<List<DailyRegistrationStatDto>> GetDailyRegistrationStatsAsync(DateTime from, DateTime to)
    {
        var rawStats = await _db.AppUsers
            .Where(u => u.CreatedAt >= from && u.CreatedAt <= to)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .ToListAsync();
            
        return rawStats.Select(s => new DailyRegistrationStatDto
        {
            Date = s.Date.ToString("yyyy-MM-dd"),
            Count = s.Count
        }).OrderBy(s => s.Date).ToList();
    }

    public async Task<List<FunnelStatDto>> GetFunnelStatsAsync(DateTime from, DateTime to)
    {
        var result = new List<FunnelStatDto>();
        
        // Loop through each day in the range
        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            
            // Get views from Redis
            var views = await _trackingService.GetPageViewsAsync(dateStr);
            
            // Get signups for that day from DB
            var nextDate = date.AddDays(1);
            var signups = await _db.AppUsers
                .CountAsync(u => u.CreatedAt >= date && u.CreatedAt < nextDate);
                
            result.Add(new FunnelStatDto 
            {
                Date = dateStr,
                LandingPageViews = views,
                Signups = signups
            });
        }
        
        return result;
    }
}
