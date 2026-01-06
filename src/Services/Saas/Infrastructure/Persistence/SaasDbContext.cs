using Microsoft.EntityFrameworkCore;
using _360Retail.Services.Saas.Domain.Entities;

namespace _360Retail.Services.Saas.Infrastructure.Persistence;

public partial class SaasDbContext : DbContext
{
    public SaasDbContext(DbContextOptions<SaasDbContext> options)
        : base(options)
    {
    }

    // ===== DbSet =====
    public virtual DbSet<Store> Stores { get; set; }
    public virtual DbSet<ServicePlan> ServicePlans { get; set; }
    public virtual DbSet<Subscription> Subscriptions { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }

    // ===== Mapping =====
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Default schema = saas
        modelBuilder.HasDefaultSchema("saas");

        // Store
        modelBuilder.Entity<Store>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("stores_pkey");

            entity.ToTable("stores");

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.StoreName)
                .HasMaxLength(200)
                .HasColumnName("store_name");

            entity.Property(e => e.Address)
                .HasColumnName("address");

            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamptz")
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ServicePlan
        modelBuilder.Entity<ServicePlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("service_plans_pkey");

            entity.ToTable("service_plans");

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.PlanName)
                .HasMaxLength(100)
                .HasColumnName("plan_name");

            entity.Property(e => e.Price)
                .HasColumnType("numeric(18,2)")
                .HasColumnName("price");

            entity.Property(e => e.DurationDays)
                .HasColumnName("duration_days");

            entity.Property(e => e.Features)
                .HasColumnType("jsonb")
                .HasColumnName("features");

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamptz")
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Subscription
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("subscriptions_pkey");

            entity.ToTable("subscriptions");

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.StoreId)
                .HasColumnName("store_id");

            entity.Property(e => e.PlanId)
                .HasColumnName("plan_id");

            entity.Property(e => e.StartDate)
                .HasColumnType("timestamptz")
                .HasColumnName("start_date");

            entity.Property(e => e.EndDate)
                .HasColumnType("timestamptz")
                .HasColumnName("end_date");

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");

            entity.Property(e => e.AutoRenew)
                .HasColumnName("auto_renew")
                .HasDefaultValue(false);

            entity.HasOne(d => d.Store)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.StoreId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("subscriptions_store_id_fkey");

            entity.HasOne(d => d.Plan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.PlanId)
                .HasConstraintName("subscriptions_plan_id_fkey");
        });

        // Payment
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payments_pkey");

            entity.ToTable("payments");

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.SubscriptionId)
                .HasColumnName("subscription_id");

            entity.Property(e => e.Amount)
                .HasColumnType("numeric(18,2)")
                .HasColumnName("amount");

            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");

            entity.Property(e => e.TransactionCode)
                .HasMaxLength(100)
                .HasColumnName("transaction_code");

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");

            entity.Property(e => e.PaymentDate)
                .HasColumnType("timestamptz")
                .HasColumnName("payment_date")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // --- Added columns (NEW) ---
            entity.Property(e => e.Provider)
                .HasMaxLength(50)
                .HasColumnName("provider");

            entity.Property(e => e.ProviderTransactionId)
                .HasMaxLength(100)
                .HasColumnName("provider_transaction_id");

            entity.Property(e => e.RequestPayload)
                .HasColumnType("jsonb")
                .HasColumnName("request_payload");

            entity.Property(e => e.ResponsePayload)
                .HasColumnType("jsonb")
                .HasColumnName("response_payload");

            entity.HasOne(d => d.Subscription)
                .WithMany(p => p.Payments)
                .HasForeignKey(d => d.SubscriptionId)
                .HasConstraintName("payments_subscription_id_fkey");
        });
    }
}
