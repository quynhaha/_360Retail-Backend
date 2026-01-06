using System.ComponentModel.DataAnnotations.Schema;

namespace _360Retail.Services.Saas.Domain.Entities;

[Table("stores", Schema = "saas")]
public class Store
{
    public Guid Id { get; set; }

    [Column("store_name")]
    public string StoreName { get; set; } = null!;

    public string? Address { get; set; }
    public string? Phone { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    public virtual ICollection<Subscription> Subscriptions { get; set; }
        = new List<Subscription>();
}
