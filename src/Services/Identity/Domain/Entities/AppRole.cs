using System;

namespace _360Retail.Services.Identity.Domain.Entities;

public partial class AppRole
{
    public Guid Id { get; set; }

    public string RoleName { get; set; } = null!;

    public string? Description { get; set; }
}
