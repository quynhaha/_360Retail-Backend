namespace _360Retail.Services.Saas.Application.DTOs.Stores;

public class CreateStoreDto
{
    public string StoreName { get; set; } = null!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
}
