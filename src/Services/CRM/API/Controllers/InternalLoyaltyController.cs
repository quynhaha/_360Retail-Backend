using Microsoft.AspNetCore.Mvc;
using _360Retail.Services.CRM.Application.Services;

namespace _360Retail.Services.CRM.API.Controllers;

/// <summary>
/// Internal APIs for cross-service communication (Sales → CRM)
/// No authentication required — internal network only
/// </summary>
[ApiController]
[Route("crm/internal")]
public class InternalLoyaltyController : ControllerBase
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly ILogger<InternalLoyaltyController> _logger;

    public InternalLoyaltyController(ILoyaltyService loyaltyService, ILogger<InternalLoyaltyController> logger)
    {
        _loyaltyService = loyaltyService;
        _logger = logger;
    }

    /// <summary>
    /// Auto-earn loyalty points after order completion
    /// Called by Sales Service after successful order creation
    /// </summary>
    [HttpPost("loyalty/earn-from-order")]
    public async Task<IActionResult> EarnFromOrder([FromBody] InternalEarnPointsRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Internal earn points: StoreId={StoreId}, CustomerId={CustomerId}, OrderId={OrderId}, Amount={Amount}",
                request.StoreId, request.CustomerId, request.OrderId, request.TotalAmount);

            await _loyaltyService.ProcessEarnPointsAsync(request.StoreId, new Application.DTOs.EarnPointsRequestDto
            {
                CustomerId = request.CustomerId,
                OrderId = request.OrderId,
                TotalAmount = request.TotalAmount,
                TotalQuantity = request.TotalQuantity
            });

            return Ok(new { success = true, message = "Points processed" });
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Customer {CustomerId} not found for earn points", request.CustomerId);
            return NotFound(new { success = false, message = "Không tìm thấy khách hàng" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing earn points for Order {OrderId}", request.OrderId);
            return StatusCode(500, new { success = false, message = "Internal error processing points" });
        }
    }
}

public class InternalEarnPointsRequest
{
    public Guid StoreId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public int TotalQuantity { get; set; }
}
