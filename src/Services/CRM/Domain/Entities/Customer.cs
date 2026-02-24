using System;
using System.Collections.Generic;

namespace _360Retail.Services.CRM.Domain.Entities;

public partial class Customer
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? ZaloId { get; set; }

    public DateTime? LastPurchaseDate { get; set; }

    public int? TotalPoints { get; set; }

    public string? Rank { get; set; }

    public Guid? StoreId { get; set; }

    public virtual ICollection<CustomerFeedback> CustomerFeedbacks { get; set; } = new List<CustomerFeedback>();

    public virtual ICollection<LoyaltyHistory> LoyaltyHistories { get; set; } = new List<LoyaltyHistory>();
    
    public virtual ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();
}
