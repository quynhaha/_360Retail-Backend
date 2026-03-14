using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _360Retail.Services.CRM.Application.DTOs;
using _360Retail.Services.CRM.Application.Services;

namespace _360Retail.Services.CRM.API.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = "StoreOwner,Manager,Staff")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    private Guid GetStoreIdFromToken()
    {
        var storeIdClaim = User.FindFirst("store_id")?.Value;
        return storeIdClaim != null ? Guid.Parse(storeIdClaim) : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _customerService.GetByStoreAsync(GetStoreIdFromToken(), page, pageSize);
        return Ok(new
        {
            data = result.Data,
            meta = new { page = result.Page, pageSize = result.PageSize, total = result.TotalCount }
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var customer = await _customerService.GetByIdAsync(id, GetStoreIdFromToken());
        if (customer == null) return NotFound(new { error = "Không tìm thấy khách hàng" });
        return Ok(new { data = customer });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        try
        {
            var customer = await _customerService.CreateAsync(GetStoreIdFromToken(), dto);
            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, new { data = customer });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerDto dto)
    {
        try
        {
            var customer = await _customerService.UpdateAsync(id, GetStoreIdFromToken(), dto);
            if (customer == null) return NotFound(new { error = "Không tìm thấy khách hàng" });
            return Ok(new { data = customer });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _customerService.DeleteAsync(id, GetStoreIdFromToken());
        if (!result) return NotFound(new { error = "Không tìm thấy khách hàng" });
        return NoContent();
    }
}
