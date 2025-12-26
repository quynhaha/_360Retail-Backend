using System;
using System.Collections.Generic;

namespace _360Retail.Services.Identity.Infrastructure.Persistence.Entities;

public partial class AppRole
{
    public Guid Id { get; set; }

    public string RoleName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
