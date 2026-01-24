using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using _360Retail.Services.HR.Domain.Entities;

namespace _360Retail.Services.HR.Infrastructure.Persistence;

public partial class HrDbContext : DbContext
{
    public HrDbContext()
    {
    }

    public HrDbContext(DbContextOptions<HrDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<WorkTask> Tasks { get; set; }

    public virtual DbSet<Timekeeping> Timekeepings { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("employees_pkey");

            entity.ToTable("employees", "hr");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.AppUserId).HasColumnName("app_user_id");
            entity.Property(e => e.BaseSalary)
                .HasPrecision(18, 2)
                .HasColumnName("base_salary");
            entity.Property(e => e.FaceData).HasColumnName("face_data");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.JoinDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("join_date");
            entity.Property(e => e.Position)
                .HasMaxLength(100)
                .HasColumnName("position");
            entity.Property(e => e.RegisteredDeviceId)
                .HasMaxLength(100)
                .HasColumnName("registered_device_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.StoreId).HasColumnName("store_id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
        });

        modelBuilder.Entity<WorkTask>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tasks_pkey");

            entity.ToTable("tasks", "hr");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.AssigneeId).HasColumnName("assignee_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Deadline)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("deadline");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Priority)
                .HasMaxLength(20)
                .HasColumnName("priority");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.StoreId).HasColumnName("store_id");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.CreatedByEmployeeId)
                .HasColumnName("created_by_employee_id");

            entity.HasOne(d => d.Assignee).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.AssigneeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tasks_assignee_id_fkey");
            
            entity.HasOne(d => d.CreatedBy)
                .WithMany()
                .HasForeignKey(d => d.CreatedByEmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tasks_created_by_employee_id_fkey");
        });

        modelBuilder.Entity<Timekeeping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("timekeepings_pkey");

            entity.ToTable("timekeepings", "hr");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.CheckInImageUrl).HasColumnName("check_in_image_url");
            entity.Property(e => e.CheckInTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("check_in_time");
            entity.Property(e => e.CheckOutTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("check_out_time");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.IsLate)
                .HasDefaultValue(false)
                .HasColumnName("is_late");
            entity.Property(e => e.LocationGps)
                .HasMaxLength(255)
                .HasColumnName("location_gps");
            entity.Property(e => e.StoreId).HasColumnName("store_id");

            entity.HasOne(d => d.Employee).WithMany(p => p.Timekeepings)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("timekeepings_employee_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
