using System;
using _360Retail.Services.CRM.Domain.Enums;

using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.CRM.Application.DTOs;

public class LoyaltyRuleDto
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string Name { get; set; } = null!;
    public LoyaltyRuleType Type { get; set; }
    public decimal EarningRate { get; set; }
    public decimal MinSpend { get; set; }
    public LoyaltyRuleStatus Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLoyaltyRuleDto
{
    [Required]
    public string Name { get; set; } = null!;
    
    [Required]
    public LoyaltyRuleType Type { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal EarningRate { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal MinSpend { get; set; }
    
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class UpdateLoyaltyRuleDto
{
    [Required]
    public string Name { get; set; } = null!;
    
    [Range(0, double.MaxValue)]
    public decimal EarningRate { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal MinSpend { get; set; }
    
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class CustomerLoyaltySummaryDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = null!;
    public int TotalPoints { get; set; }
    public string Rank { get; set; } = null!;
}

public class EarnPointsRequestDto
{
    [Required]
    public Guid CustomerId { get; set; }
    
    [Required]
    public Guid OrderId { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }
    
    [Range(0, int.MaxValue)]
    public int TotalQuantity { get; set; }
}

public class RedeemPointsRequestDto
{
    [Required]
    public Guid CustomerId { get; set; }
    
    [Range(1, int.MaxValue)]
    public int PointsToRedeem { get; set; }
}

public class LoyaltyTransactionDto
{
    public Guid Id { get; set; }
    public int Points { get; set; }
    public LoyaltyTransactionType Type { get; set; }
    public string Description { get; set; } = null!;
    public DateTime TransactionDate { get; set; }
}

public record PagedResult<T>(
    IEnumerable<T> Data,
    int Page,
    int PageSize,
    int TotalCount
);

// --- Customer DTOs ---

public class CustomerDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? ZaloId { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
    public int TotalPoints { get; set; }
    public string? Rank { get; set; }
    public Guid? StoreId { get; set; }
}

public class CreateCustomerDto
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = null!;

    [MaxLength(100)]
    public string? ZaloId { get; set; }
}

public class UpdateCustomerDto
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = null!;

    [MaxLength(100)]
    public string? ZaloId { get; set; }
}
