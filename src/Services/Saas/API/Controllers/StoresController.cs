using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using _360Retail.Services.Saas.Application.DTOs.Stores;
using _360Retail.Services.Saas.Application.Interfaces;
using _360Retail.Services.Saas.Infrastructure.HttpClients;


namespace _360Retail.Services.Saas.API.Controllers;

[ApiController]
[Route("api/saas/stores")]
[Authorize]
public class StoresController : ControllerBase
{
    private readonly IStoreService _storeService;
    private readonly IIdentityClient _identityClient;

    public StoresController(
        IStoreService storeService,
        IIdentityClient identityClient)
    {
        _storeService = storeService;
        _identityClient = identityClient;
    }

    // CREATE
    [Authorize(Roles = "SuperAdmin,StoreOwner")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateStoreDto dto)
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        var accessToken = authHeader.Replace("Bearer ", "");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roles = User.FindAll(ClaimTypes.Role)
                        .Select(r => r.Value)
                        .ToList();

        var store = await _storeService.CreateAsync(
            Guid.Parse(userId),
            dto
        );

        if (roles.Contains("StoreOwner"))
        {
            await _identityClient.AssignStoreAsync(
                accessToken,
                store.Id
            );
        }

        return Ok(store);
    }


    // READ ALL
    [Authorize(Roles = "SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _storeService.GetAllAsync());
    }

    // READ ONE
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var store = await _storeService.GetByIdAsync(id);
        return store == null ? NotFound() : Ok(store);
    }

    // UPDATE
    [Authorize(Roles = "SuperAdmin,StoreOwner")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateStoreDto dto)
    {
        var roles = User.FindAll(ClaimTypes.Role)
                        .Select(r => r.Value)
                        .ToList();
        if (roles.Contains("SuperAdmin"))
        {
            var success = await _storeService.UpdateAsync(id, dto);
            return success ? Ok() : NotFound();
        }
        var token = Request.Headers["Authorization"]
            .ToString()
            .Replace("Bearer ", "");

        var hasAccess = await _identityClient.HasStoreAccessAsync(
            token,
            id,
            "Owner"
        );

        if (!hasAccess)
            return Forbid();

        var result = await _storeService.UpdateAsync(id, dto);
        return result ? Ok() : NotFound();
    }

    // DELETE
    [Authorize(Roles = "SuperAdmin,StoreOwner")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var roles = User.FindAll(ClaimTypes.Role)
                        .Select(r => r.Value)
                        .ToList();

        if (roles.Contains("SuperAdmin"))
        {
            var success = await _storeService.DeleteAsync(id);
            return success ? Ok() : NotFound();
        }

        var token = Request.Headers["Authorization"]
            .ToString()
            .Replace("Bearer ", "");

        var hasAccess = await _identityClient.HasStoreAccessAsync(
            token,
            id,
            "Owner"
        );

        if (!hasAccess)
            return Forbid();

        var result = await _storeService.DeleteAsync(id);
        return result ? Ok() : NotFound();
    }
}
