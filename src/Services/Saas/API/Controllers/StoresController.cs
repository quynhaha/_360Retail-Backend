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
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        return Ok(await _storeService.GetAllAsync(includeInactive));
    }

    // GET MY OWNED STORES (for Store Owner)
    [Authorize(Roles = "StoreOwner")]
    [HttpGet("my-owned-stores")]
    public async Task<IActionResult> GetMyOwnedStores([FromQuery] bool includeInactive = false)
    {
        var token = Request.Headers["Authorization"]
            .ToString()
            .Replace("Bearer ", "");

        // Get all stores user has access to from Identity Service
        var userStores = await _identityClient.GetMyStoresAsync(token);

        // Filter only stores with "Owner" role
        var ownedStoreIds = userStores
            .Where(s => s.RoleInStore == "Owner")
            .Select(s => s.StoreId)
            .ToList();

        if (!ownedStoreIds.Any())
            return Ok(new List<object>());

        // Get full store details from Saas DB (include inactive if requested)
        var stores = await _storeService.GetByIdsAsync(ownedStoreIds, includeInactive);

        // Combine with access info
        var result = stores.Select(store => new
        {
            store.Id,
            store.StoreName,
            store.Address,
            store.Phone,
            store.IsActive,
            store.CreatedAt,
            IsDefault = userStores.FirstOrDefault(s => s.StoreId == store.Id)?.IsDefault ?? false
        });

        return Ok(result);
    }

    // READ ONE
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var store = await _storeService.GetByIdAsync(id);
        return store == null ? NotFound() : Ok(store);
    }

    // GET MY CURRENT STORE (for Manager/Staff)
    [Authorize]
    [HttpGet("my-store")]
    public async Task<IActionResult> GetMyCurrentStore()
    {
        // Get store_id from JWT claims (set during login)
        var storeIdClaim = User.FindFirst("store_id")?.Value;
        
        if (string.IsNullOrEmpty(storeIdClaim) || !Guid.TryParse(storeIdClaim, out var storeId))
            return BadRequest(new { message = "User is not assigned to any store" });

        var store = await _storeService.GetByIdAsync(storeId);
        if (store == null)
            return NotFound(new { message = "Store not found" });

        // Also include role in store from JWT
        var roleInStore = User.FindFirst("store_role")?.Value ?? "Unknown";

        return Ok(new
        {
            store.Id,
            store.StoreName,
            store.Address,
            store.Phone,
            store.IsActive,
            store.CreatedAt,
            YourRole = roleInStore
        });
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
