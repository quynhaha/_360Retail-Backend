using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace _360Retail.Services.Sales.Infrastructure.HttpClients;

public class CrmClient : ICrmClient
{
    private readonly HttpClient _http;
    private readonly ILogger<CrmClient> _logger;

    public CrmClient(HttpClient http, ILogger<CrmClient> logger, IConfiguration config)
    {
        _http = http;
        _logger = logger;

        // Add internal API key for cross-service authentication
        var internalKey = config["InternalApi:Key"] ?? "360retail-internal-secret-key";
        _http.DefaultRequestHeaders.Add("X-Internal-Key", internalKey);
    }

    public async Task<bool> EarnPointsFromOrderAsync(
        Guid storeId, Guid customerId, 
        Guid orderId, decimal totalAmount, int totalQuantity)
    {
        try
        {
            var payload = new
            {
                storeId,
                customerId,
                orderId,
                totalAmount,
                totalQuantity
            };

            var response = await _http.PostAsJsonAsync(
                "/crm/internal/loyalty/earn-from-order", payload);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Loyalty points earned for Order {OrderId}, Customer {CustomerId}",
                    orderId, customerId);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "CRM earn points failed for Order {OrderId}: [{Status}] {Error}",
                orderId, response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            // Fire-and-forget: log but don't fail the order
            _logger.LogError(ex, 
                "CRM service unavailable for earn points. Order {OrderId} still valid.",
                orderId);
            return false;
        }
    }
}
