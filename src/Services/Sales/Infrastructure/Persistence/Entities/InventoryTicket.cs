using System;
using System.Collections.Generic;

namespace _360Retail.Services.Sales.Infrastructure.Persistence.Entities;

public partial class InventoryTicket
{
    public Guid Id { get; set; }

    public Guid StoreId { get; set; }

    public string? Code { get; set; }

    public string? Type { get; set; }

    public Guid? CreatedByEmployeeId { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }
}
