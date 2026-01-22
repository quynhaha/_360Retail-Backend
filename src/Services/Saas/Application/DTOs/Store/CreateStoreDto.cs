namespace _360Retail.Services.Saas.Application.DTOs.Stores;

public class CreateStoreDto
{
    public string StoreName { get; set; } = null!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    
    /// <summary>
    /// Required for paid users to purchase subscription for new store
    /// </summary>
    public Guid? PlanId { get; set; }
}
