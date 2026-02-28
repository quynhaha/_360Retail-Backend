using System.ComponentModel.DataAnnotations.Schema;

namespace _360Retail.Services.Saas.Domain.Entities;

/// <summary>
/// Store owner reviews a subscription plan
/// </summary>
[Table("plan_reviews", Schema = "saas")]
public class PlanReview
{
    public Guid Id { get; set; }

    [Column("plan_id")]
    public Guid PlanId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("store_id")]
    public Guid StoreId { get; set; }

    [Column("rating")]
    public int Rating { get; set; }

    [Column("content")]
    public string? Content { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public virtual ServicePlan Plan { get; set; } = null!;
    public virtual Store Store { get; set; } = null!;
}
