using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace _360Retail.Services.Identity.Domain.Entities
{
    [Table("user_store_access", Schema = "identity")]
    public class UserStoreAccess
    {
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("store_id")]
        public Guid StoreId { get; set; }

        [Column("role_in_store")]
        public string RoleInStore { get; set; } = "Staff";

        [Column("is_default")]
        public bool IsDefault { get; set; }

        [Column("assigned_at")]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public virtual AppUser User { get; set; } = null!;
    }
}
