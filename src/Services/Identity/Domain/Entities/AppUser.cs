using System;
using System.Collections.Generic;

namespace _360Retail.Services.Identity.Domain.Entities;

public partial class AppUser
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string? PhoneNumber { get; set; }

    public string Status { get; set; } = "Pending";

    public bool IsActivated { get; set; } = false;

    public bool MustChangePassword { get; set; } = false;

    public Guid? StoreId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<AppRole> Roles { get; set; } = new List<AppRole>();

    public virtual ICollection<UserStoreAccess> StoreAccesses { get; set; }
        = new List<UserStoreAccess>();
}
