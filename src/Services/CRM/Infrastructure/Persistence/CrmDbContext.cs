using System;
using System.Collections.Generic;
using _360Retail.Services.CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace _360Retail.Services.CRM.Infrastructure.Persistence;

public partial class CrmDbContext : DbContext
{


    public CrmDbContext(DbContextOptions<CrmDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<CustomerFeedback> CustomerFeedbacks { get; set; }

    public virtual DbSet<LoyaltyHistory> LoyaltyHistories { get; set; }
    public virtual DbSet<LoyaltyRule> LoyaltyRules { get; set; }
    public virtual DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }
    public virtual DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("customers_pkey");

            entity.ToTable("customers", "crm");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.LastPurchaseDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_purchase_date");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phone_number");
            entity.Property(e => e.Rank)
                .HasMaxLength(50)
                .HasColumnName("rank");
            entity.Property(e => e.StoreId).HasColumnName("store_id");
            entity.Property(e => e.TotalPoints)
                .HasDefaultValue(0)
                .HasColumnName("total_points");
            entity.Property(e => e.ZaloId)
                .HasMaxLength(100)
                .HasColumnName("zalo_id");
            
            // Should match DB
            entity.Ignore(e => e.LoyaltyTransactions);
        });

        modelBuilder.Entity<CustomerFeedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("customer_feedbacks_pkey");

            entity.ToTable("customer_feedbacks", "crm");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByEmployeeId).HasColumnName("created_by_employee_id");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.Source)
                .HasMaxLength(50)
                .HasColumnName("source");
            entity.Property(e => e.StoreId).HasColumnName("store_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");

            entity.HasIndex(e => e.OrderId)
                .IsUnique()
                .HasFilter("order_id IS NOT NULL");

            entity.HasOne(d => d.Customer).WithMany(p => p.CustomerFeedbacks)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("customer_feedbacks_customer_id_fkey");
        });

        modelBuilder.Entity<LoyaltyHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("loyalty_history_pkey");

            entity.ToTable("loyalty_history", "crm");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .HasColumnName("id");
            entity.Property(e => e.ChangeDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("change_date");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PointsChanged).HasColumnName("points_changed");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");

            entity.HasOne(d => d.Customer).WithMany(p => p.LoyaltyHistories)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("loyalty_history_customer_id_fkey");
        });

        modelBuilder.Entity<LoyaltyRule>(entity =>
        {
            entity.ToTable("loyalty_rules", "crm");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EarningRate).HasColumnType("decimal(18,4)");
            entity.Property(e => e.MinSpend).HasColumnType("decimal(18,4)");
        });

        modelBuilder.Entity<LoyaltyTransaction>(entity =>
        {
            entity.ToTable("loyalty_transactions", "crm");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId).IsUnique(); // Unique Index
            entity.Property(e => e.Points).IsRequired();
        });
        
        // Configure Idempotency
        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.ToTable("idempotency_records", "crm");
            entity.HasKey(e => e.Key);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
