namespace _360Retail.Services.Sales.Infrastructure.HttpClients;

public interface ICrmClient
{
    /// <summary>
    /// Call CRM internal endpoint to earn loyalty points from order
    /// </summary>
    Task<bool> EarnPointsFromOrderAsync(Guid storeId, Guid customerId, 
        Guid orderId, decimal totalAmount, int totalQuantity);
}
