using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

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

    // OAuth Provider Info
    [Column("auth_provider")]
    public string AuthProvider { get; set; } = "Local";  // "Local", "Google", "Facebook"
    
    [Column("external_user_id")]
    public string? ExternalUserId { get; set; }  // Provider's user ID
    
    [Column("profile_picture_url")]
    public string? ProfilePictureUrl { get; set; }  // Avatar from provider

    // Trial period fields
    [Column("trial_start_date")]
    public DateTime? TrialStartDate { get; set; }
    
    [Column("trial_end_date")]
    public DateTime? TrialEndDate { get; set; }
    
    /// <summary>
    /// Check if user is currently in active trial period
    /// </summary>
    [NotMapped]
    public bool IsTrialActive => TrialEndDate.HasValue && TrialEndDate.Value > DateTime.UtcNow;
    
    /// <summary>
    /// Days remaining in trial (0 if expired or not in trial)
    /// </summary>
    [NotMapped]
    public int TrialDaysRemaining => TrialEndDate.HasValue 
        ? Math.Max(0, (int)(TrialEndDate.Value - DateTime.UtcNow).TotalDays) 
        : 0;

    public virtual ICollection<AppRole> Roles { get; set; } = new List<AppRole>();

    public virtual ICollection<UserStoreAccess> StoreAccesses { get; set; }
        = new List<UserStoreAccess>();
}

