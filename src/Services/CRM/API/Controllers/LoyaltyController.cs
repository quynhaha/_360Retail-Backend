using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _360Retail.Services.CRM.Application.DTOs;
using _360Retail.Services.CRM.Application.Services;
using _360Retail.Services.CRM.Domain.Entities;
using _360Retail.Services.CRM.Application.Interfaces;

namespace _360Retail.Services.CRM.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize(Roles = "StoreOwner,Manager")]
public class LoyaltyController : ControllerBase
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly ILoyaltyRuleRepository _ruleRepo;
    private readonly IMapper _mapper;

    public LoyaltyController(ILoyaltyService loyaltyService, ILoyaltyRuleRepository ruleRepo, IMapper mapper)
    {
        _loyaltyService = loyaltyService;
        _ruleRepo = ruleRepo;
        _mapper = mapper;
    }

    private Guid GetStoreIdFromToken()
    {
        var storeIdClaim = User.FindFirst("store_id")?.Value;
        if (string.IsNullOrEmpty(storeIdClaim) || !Guid.TryParse(storeIdClaim, out var storeId))
        {
            throw new UnauthorizedAccessException("StoreId claim is missing or invalid in token.");
        }
        return storeId;
    }

    // --- RULES ---

    [HttpGet("loyalty-rules")]
    public async Task<IActionResult> GetRules()
    {
        var rules = await _ruleRepo.GetByStoreIdAsync(GetStoreIdFromToken());
        return Ok(new { data = rules });
    }

    [HttpPost("loyalty-rules")]
    public async Task<IActionResult> CreateRule([FromBody] CreateLoyaltyRuleDto dto)
    {
        var rule = _mapper.Map<LoyaltyRule>(dto);
        rule.StoreId = GetStoreIdFromToken();
        
        await _ruleRepo.AddAsync(rule);
        var responseDto = _mapper.Map<LoyaltyRuleDto>(rule);
        
        return CreatedAtAction(nameof(GetRule), new { id = rule.Id }, new { data = responseDto });
    }

    [HttpGet("loyalty-rules/{id}")]
    public async Task<IActionResult> GetRule(Guid id)
    {
        var rule = await _ruleRepo.GetByIdAsync(id);
        if (rule == null || rule.StoreId != GetStoreIdFromToken()) return NotFound();
        
        return Ok(new { data = _mapper.Map<LoyaltyRuleDto>(rule) });
    }

    [HttpDelete("loyalty-rules/{id}")]
    public async Task<IActionResult> DeleteRule(Guid id)
    {
        var rule = await _ruleRepo.GetByIdAsync(id);
        if (rule == null || rule.StoreId != GetStoreIdFromToken()) return NotFound();

        await _ruleRepo.DeleteAsync(id);
        return NoContent();
    }

    // --- CUSTOMER LOYALTY ---

    [HttpGet("customers/{customerId}/loyalty-summary")]
    public async Task<IActionResult> GetSummary(Guid customerId)
    {
        try
        {
            var summary = await _loyaltyService.GetCustomerSummaryAsync(customerId);
            return Ok(new { data = summary });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Customer not found" });
        }
    }

    [HttpGet("customers/{customerId}/loyalty-transactions")]
    public async Task<IActionResult> GetTransactions(Guid customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _loyaltyService.GetTransactionsAsync(customerId, page, pageSize);
        return Ok(new { data = result.Data, meta = new { page = result.Page, pageSize = result.PageSize, total = result.TotalCount } });
    }

    [HttpPost("customers/{customerId}/redeem")]
    public async Task<IActionResult> RedeemPoints(Guid customerId, [FromBody] RedeemPointsRequestDto request)
    {
        if (customerId != request.CustomerId) return BadRequest("Customer ID mismatch");

        // Use store ID from token if required by ProcessRedeemPointsAsync. Wait, we should attach it.
        // Wait, request struct doesn't have StoreId anymore. I will need to set it or modify it. 
        // Actually LoyaltyService ProcessRedeemPointsAsync takes a RedeemPointsRequestDto but we removed StoreId from DTOs.
        // I will change the controller to just pass what it has. Since RedeemPointsRequestDto doesn't have StoreId, 
        // maybe the service needs adaptation? I'll check my CrmDtos.cs replacement.
        // I'll keep this simple and let the C# compiler complain if I missed a field, then I'll fix it.
        
        try
        {
            await _loyaltyService.ProcessRedeemPointsAsync(GetStoreIdFromToken(), request);
            return Ok(new { message = "Points redeemed successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Customer not found" });
        }
    }

    // --- INTERNAL / SALES INTEGRATION ---

    [HttpPost("loyalty/earn-from-order")]
    public async Task<IActionResult> EarnFromOrder([FromBody] EarnPointsRequestDto request)
    {
        try
        {
            // The loyalty service currently expects EarnPointsRequestDto. Since I removed StoreId from it, 
            // I need to either put it back or pass StoreId separately. I will pass the StoreId from token.
            // Wait, does ProcessEarnPointsAsync take StoreId as parameter or inside DTO? 
            // The DTO had it. Let's fix the request by setting it. Wait, the DTO doesn't have StoreId property at all anymore.
            // I will use another tool to fix LoyaltyService to take storeId as parameter.
            // For now, I'll pass storeId alongside request. Wait, DTO was already defined without StoreId.
            // Let's modify EarnPointsRequestDto implicitly again or just pass GetStoreIdFromToken() into service.
            
            // Assuming I will update LoyaltyService to: Task ProcessEarnPointsAsync(Guid storeId, EarnPointsRequestDto request)
            await _loyaltyService.ProcessEarnPointsAsync(GetStoreIdFromToken(), request);
            return Ok(new { message = "Points processed" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Customer not found" });
        }
    }
}
