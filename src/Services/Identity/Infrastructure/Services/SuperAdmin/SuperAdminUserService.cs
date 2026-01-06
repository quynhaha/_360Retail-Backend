using Microsoft.EntityFrameworkCore;
using _360Retail.Services.Identity.Application.DTOs.SuperAdmin;
using _360Retail.Services.Identity.Application.Interfaces.SuperAdmin;
using _360Retail.Services.Identity.Domain.Entities;
using _360Retail.Services.Identity.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace _360Retail.Services.Identity.Infrastructure.Services.SuperAdmin;

public class SuperAdminUserService : ISuperAdminUserService
{
    private readonly IdentityDbContext _db;

    public SuperAdminUserService(IdentityDbContext db)
    {
        _db = db;
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
            throw new Exception("User not found");

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

 
    // CREATE USER (NOT SUPERADMIN)
    public async Task<Guid> CreateAsync(CreateUserDto dto)
    {
        if (dto.RoleName == "SuperAdmin")
            throw new Exception("Cannot create SuperAdmin via API");

        if (await _db.AppUsers.AnyAsync(u => u.Email == dto.Email))
            throw new Exception("Email already exists");

        var role = await _db.AppRoles
            .FirstOrDefaultAsync(r => r.RoleName == dto.RoleName);

        if (role == null)
            throw new Exception("Invalid role");

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            UserName = dto.Email,
            PasswordHash = HashPassword(dto.Password),
            Status = "Active",
            IsActivated = true,
            CreatedAt = DateTime.UtcNow
        };

        user.Roles.Add(role);

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        return user.Id;
    }

 
    // UPDATE USER
    public async Task UpdateAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            throw new Exception("User not found");

        user.IsActivated = dto.IsActivated;
        user.Status = dto.Status;

        await _db.SaveChangesAsync();
    }

    // DELETE USER
    public async Task DeleteAsync(Guid id)
    {
        var user = await _db.AppUsers
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            throw new Exception("User not found");

        // ❗ Không cho xoá SuperAdmin
        if (user.Roles.Any(r => r.RoleName == "SuperAdmin"))
            throw new Exception("Cannot delete SuperAdmin");

        _db.AppUsers.Remove(user);
        await _db.SaveChangesAsync();
    }

    // PASSWORD HASH (reuse logic)
    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(
            sha.ComputeHash(Encoding.UTF8.GetBytes(password))
        );
    }
}
