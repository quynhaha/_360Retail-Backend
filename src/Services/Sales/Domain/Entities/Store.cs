using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _360Retail.Services.Sales.Domain.Entities;

[Table("stores", Schema = "sales")]
public partial class Store
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("store_name")]
    public string StoreName { get; set; } = null!;

    [Column("address")]
    public string? Address { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }
}
