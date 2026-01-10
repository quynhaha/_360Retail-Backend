using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using _360Retail.Services.Sales.Application.DTOs;
using _360Retail.Services.Sales.Application.Interfaces;

namespace _360Retail.Services.Sales.API.Controllers;

[Authorize]
[Route("api/sales/orders")]
public class OrdersController : BaseApiController
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        if (!ModelState.IsValid) return BadResult("Invalid data");
        
        var storeId = GetCurrentStoreId();
        if (storeId == Guid.Empty) return BadResult("User has no store context");

        // Try standard claim
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // Fallback to custom claim
        if (string.IsNullOrEmpty(userIdStr)) userIdStr = User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        
        try 
        {
            var userId = Guid.Parse(userIdStr);
            var orderId = await _orderService.CreateAsync(dto, storeId, userId);
            return OkResult(orderId, "Order created successfully");
        }
        catch (Exception ex)
        {
            return BadResult(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var storeId = GetCurrentStoreId();
        if (storeId == Guid.Empty) return BadResult("User has no store context");

        var result = await _orderService.GetListAsync(
            storeId, status, fromDate, toDate, page, pageSize);
            
        return OkResult(result);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var storeId = GetCurrentStoreId();
        if (storeId == Guid.Empty) return BadResult("User has no store context");

        var order = await _orderService.GetByIdAsync(id, storeId);
        if (order == null) return NotFound();
        
        return OkResult(order);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] string status)
    {
        if (string.IsNullOrEmpty(status)) return BadResult("Status is required");

        var storeId = GetCurrentStoreId();
        if (storeId == Guid.Empty) return BadResult("User has no store context");

        try
        {
            await _orderService.UpdateStatusAsync(id, storeId, status);
            return OkResult(true, "Status updated");
        }
        catch (Exception ex)
        {
            return BadResult(ex.Message);
        }
    }
}
