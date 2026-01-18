namespace _360Retail.Services.Saas.Application.DTOs.Stores;

/// <summary>
/// DTO for partial update - only non-null fields will be updated
/// </summary>
public class UpdateStoreDto
{
    /// <summary>
    /// Store name - null means keep existing value
    /// </summary>
    public string? StoreName { get; set; }

    /// <summary>
    /// Address - null means keep existing value
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Phone - null means keep existing value
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Active status - null means keep existing value
    /// </summary>
    public bool? IsActive { get; set; }
}
