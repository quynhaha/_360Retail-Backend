using System;
using System.Collections.Generic;

namespace _360Retail.Services.Identity.Infrastructure.Persistence.Entities;

public partial class AppUser
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public bool? IsActive { get; set; }

    public Guid? StoreId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<AppRole> Roles { get; set; } = new List<AppRole>();
}
