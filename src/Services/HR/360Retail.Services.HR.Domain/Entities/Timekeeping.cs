using System;
using System.Collections.Generic;

namespace _360Retail.Services.HR.Domain.Entities;

public partial class Timekeeping
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public Guid StoreId { get; set; }

    public DateTime? CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public string? LocationGps { get; set; }

    public string? CheckInImageUrl { get; set; }

    public bool? IsLate { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
