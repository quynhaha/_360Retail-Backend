using _360Retail.Services.Sales.Application.DTOs;
using _360Retail.Services.Sales.Application.Interfaces;
using _360Retail.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _360Retail.Services.Sales.API.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Roles = "StoreOwner,Manager,Staff")]
[RequiresActiveSubscription]
[RequiresFeature("has_inventory_tickets")]
public class InventoryController : BaseApiController
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// Create a new inventory ticket (Import/Export). Status = Draft.
    /// Only StoreOwner and Manager can create.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "StoreOwner,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateInventoryTicketDto dto)
    {
        var storeId = GetCurrentStoreId();
        var userId = GetCurrentUserId();
        var ticketId = await _inventoryService.CreateTicketAsync(dto, storeId, userId);
        return OkResult(ticketId, "Inventory ticket created successfully");
    }

    /// <summary>
    /// Get all inventory tickets for the current store (filter by type, status, paging)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? type,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var storeId = GetCurrentStoreId();
        var result = await _inventoryService.GetAllAsync(storeId, type, status, page, pageSize);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Get inventory ticket details by ID (including items)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var storeId = GetCurrentStoreId();
        var ticket = await _inventoryService.GetByIdAsync(id, storeId);
        if (ticket == null)
            return NotFound(new { success = false, message = "Không tìm thấy phiếu kho" });

        return Ok(new { success = true, data = ticket });
    }

    /// <summary>
    /// Confirm a Draft ticket → updates product stock (Import: +stock, Export: -stock)
    /// Only StoreOwner and Manager can confirm.
    /// </summary>
    [HttpPut("{id}/confirm")]
    [Authorize(Roles = "StoreOwner,Manager")]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var storeId = GetCurrentStoreId();
        var userId = GetCurrentUserId();
        await _inventoryService.ConfirmTicketAsync(id, storeId, userId);
        return OkResult(true, "Inventory ticket confirmed and stock updated");
    }

    /// <summary>
    /// Cancel a Draft ticket (no stock changes)
    /// Only StoreOwner and Manager can cancel.
    /// </summary>
    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "StoreOwner,Manager")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var storeId = GetCurrentStoreId();
        await _inventoryService.CancelTicketAsync(id, storeId);
        return OkResult(true, "Inventory ticket cancelled");
    }

    /// <summary>
    /// Delete a Draft or Cancelled ticket permanently.
    /// Confirmed tickets cannot be deleted.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "StoreOwner,Manager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var storeId = GetCurrentStoreId();
        await _inventoryService.DeleteTicketAsync(id, storeId);
        return OkResult(true, "Inventory ticket deleted");
    }
}
