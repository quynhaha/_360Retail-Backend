using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using _360Retail.Services.CRM.Domain.Enums;

namespace _360Retail.Services.CRM.Domain.Entities;

public class LoyaltyRule
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid StoreId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public LoyaltyRuleType Type { get; set; }
    
    // Config for rule: e.g., "1 point per 1000 currency" -> EarningRate = 0.001
    public decimal EarningRate { get; set; }
    
    public decimal MinSpend { get; set; }
    
    public LoyaltyRuleStatus Status { get; set; } = LoyaltyRuleStatus.Active;
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
    
    public bool IsDeleted { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
}

public class LoyaltyTransaction
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid CustomerId { get; set; }
    
    public Guid StoreId { get; set; }
    
    public Guid? OrderId { get; set; }
    
    public Guid? RuleId { get; set; }
    
    public int Points { get; set; } // Positive for earn, negative for redeem
    
    public LoyaltyTransactionType Type { get; set; }
    
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; }
}

public class IdempotencyRecord
{
    [Key]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;
    
    public int StatusCode { get; set; }
    
    public string ResponseBody { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
}
