namespace _360Retail.Services.Identity.Application.DTOs.SuperAdmin;

/// <summary>
/// DTO for partial update - only non-null fields will be updated
/// </summary>
public class UpdateUserDto
{
    /// <summary>
    /// Activation status - null means keep existing value
    /// </summary>
    public bool? IsActivated { get; set; }

    /// <summary>
    /// User status - null means keep existing value
    /// </summary>
    public string? Status { get; set; }
}
