using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using _360Retail.Services.Saas.Application.DTOs.Stores;
using _360Retail.Services.Saas.Application.Interfaces;

namespace _360Retail.Services.Saas.API.Controllers;

[ApiController]
[Route("api/saas/stores")]
[Authorize]
public class StoresController : ControllerBase
{
    private readonly IStoreService _storeService;

    public StoresController(IStoreService storeService)
    {
        _storeService = storeService;
    }

    // CREATE
    [HttpPost]
    public async Task<IActionResult> Create(CreateStoreDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var result = await _storeService.CreateAsync(Guid.Parse(userId), dto);
        return Ok(result);
    }

    // READ ALL
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _storeService.GetAllAsync());
    }

    // READ ONE
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var store = await _storeService.GetByIdAsync(id);
        return store == null ? NotFound() : Ok(store);
    }

    // UPDATE
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateStoreDto dto)
    {
        var success = await _storeService.UpdateAsync(id, dto);
        return success ? Ok() : NotFound();
    }

    // DELETE
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _storeService.DeleteAsync(id);
        return success ? Ok() : NotFound();
    }
}
