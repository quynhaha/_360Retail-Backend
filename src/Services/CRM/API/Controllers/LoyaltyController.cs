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
using _360Retail.Shared.Filters;

namespace _360Retail.Services.CRM.API.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = "StoreOwner,Manager")]
[RequiresFeature("has_loyalty")]
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

    [HttpPut("loyalty-rules/{id}")]
    public async Task<IActionResult> UpdateRule(Guid id, [FromBody] UpdateLoyaltyRuleDto dto)
    {
        var rule = await _ruleRepo.GetByIdAsync(id);
        if (rule == null || rule.StoreId != GetStoreIdFromToken()) return NotFound();

        rule.Name = dto.Name;
        rule.EarningRate = dto.EarningRate;
        rule.MinSpend = dto.MinSpend;
        rule.StartDate = dto.StartDate;
        rule.EndDate = dto.EndDate;

        await _ruleRepo.UpdateAsync(rule);
        var responseDto = _mapper.Map<LoyaltyRuleDto>(rule);
        return Ok(new { data = responseDto });
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

    // --- INTERNAL: Called by Sales Service after order completion ---

    [HttpPost("loyalty/earn-from-order")]
    public async Task<IActionResult> EarnFromOrder([FromBody] EarnPointsRequestDto request)
    {
        try
        {
            await _loyaltyService.ProcessEarnPointsAsync(GetStoreIdFromToken(), request);
            return Ok(new { message = "Points processed" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Customer not found" });
        }
    }
}
