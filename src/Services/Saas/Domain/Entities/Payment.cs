using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace _360Retail.Services.Saas.Domain.Entities;

public partial class Payment
{
    public Guid Id { get; set; }

    public Guid SubscriptionId { get; set; }

    public decimal Amount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? TransactionCode { get; set; }

    public string? Status { get; set; }

    public DateTime? PaymentDate { get; set; }

    public virtual Subscription Subscription { get; set; } = null!;
    public string? Provider { get; set; }

    [Column("provider_transaction_id")]
    public string? ProviderTransactionId { get; set; }

    [Column("request_payload")]
    public string? RequestPayload { get; set; }

    [Column("response_payload")]
    public string? ResponsePayload { get; set; }

}
