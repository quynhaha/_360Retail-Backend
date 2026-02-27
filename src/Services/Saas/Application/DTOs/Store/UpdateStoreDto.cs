using System.ComponentModel.DataAnnotations;

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

    /// <summary>
    /// Store latitude from Google Maps (-90 to 90). Null = keep existing.
    /// </summary>
    [Range(-90, 90, ErrorMessage = "Latitude phải từ -90 đến 90")]
    public double? Latitude { get; set; }

    /// <summary>
    /// Store longitude from Google Maps (-180 to 180). Null = keep existing.
    /// </summary>
    [Range(-180, 180, ErrorMessage = "Longitude phải từ -180 đến 180")]
    public double? Longitude { get; set; }
}
