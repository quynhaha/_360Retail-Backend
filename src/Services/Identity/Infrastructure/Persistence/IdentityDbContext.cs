using System;
using _360Retail.Services.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace _360Retail.Services.Identity.Infrastructure.Persistence;

public partial class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options) { }

    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AppRole> AppRoles => Set<AppRole>();
    public DbSet<UserStoreAccess> UserStoreAccess => Set<UserStoreAccess>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        // ----------------- APP ROLE -----------------
        modelBuilder.Entity<AppRole>(entity =>
        {
            entity.ToTable("app_roles", "identity");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.RoleName)
                .HasColumnName("role_name")
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(255);
        });

        // ----------------- APP USER -----------------
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("app_users", "identity");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.UserName)
                .HasColumnName("user_name")
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(100);

            entity.Property(e => e.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(255);

            entity.Property(e => e.PhoneNumber)
                .HasColumnName("phone_number")
                .HasMaxLength(20);

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(30)
                .HasDefaultValue("Pending");

            entity.Property(e => e.IsActivated)
                .HasColumnName("is_activated")
                .HasDefaultValue(false);

            entity.Property(e => e.ActivationToken)
                .HasColumnName("activation_token")
                .HasMaxLength(100);

            entity.Property(e => e.ActivationTokenExpiredAt)
                .HasColumnName("activation_token_expired_at");

            entity.Property(e => e.StoreId)
                .HasColumnName("store_id");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ----------------- USER ROLE (SYSTEM) -----------------
        modelBuilder.Entity<AppUser>()
            .HasMany(u => u.Roles)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "user_roles",
                r => r.HasOne<AppRole>().WithMany().HasForeignKey("role_id"),
                l => l.HasOne<AppUser>().WithMany().HasForeignKey("user_id"),
                j =>
                {
                    j.ToTable("user_roles", "identity");
                    j.HasKey("user_id", "role_id");
                });

        // ----------------- USER STORE ACCESS -----------------
        modelBuilder.Entity<UserStoreAccess>(entity =>
        {
            entity.ToTable("user_store_access", "identity");
            entity.HasKey(e => new { e.UserId, e.StoreId });

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.StoreId).HasColumnName("store_id");

            entity.Property(e => e.RoleInStore)
                .HasColumnName("role_in_store")
                .HasMaxLength(50)
                .HasDefaultValue("Staff");

            entity.Property(e => e.IsDefault)
                .HasColumnName("is_default")
                .HasDefaultValue(false);

            entity.Property(e => e.AssignedAt)
                .HasColumnName("assigned_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.User)
                .WithMany(u => u.StoreAccesses)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
