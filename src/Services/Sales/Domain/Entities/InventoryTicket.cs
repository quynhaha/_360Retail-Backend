using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace _360Retail.Services.Sales.Domain.Entities;

[Table("inventory_tickets", Schema = "sales")]
public partial class InventoryTicket
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("store_id")]
    public Guid StoreId { get; set; }

    [Column("code")]
    [StringLength(50)]
    public string? Code { get; set; }

    [Column("type")]
    [StringLength(50)]
    public string? Type { get; set; }

    [Column("created_by_employee_id")]
    public Guid? CreatedByEmployeeId { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }
}
