using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _360Retail.Services.Saas.Domain.Entities;
using _360Retail.Services.Saas.Infrastructure.Persistence;

namespace _360Retail.Services.Saas.API.Controllers.SuperAdmin;

/// <summary>
/// CRUD Service Plans — SuperAdmin only
/// </summary>
[ApiController]
[Route("api/super-admin/saas/plans")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminPlansController : ControllerBase
{
    private readonly SaasDbContext _db;

    public SuperAdminPlansController(SaasDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get all plans (including inactive)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var plans = await _db.ServicePlans
            .OrderBy(p => p.Price)
            .Select(p => new
            {
                p.Id,
                p.PlanName,
                p.Price,
                p.DurationDays,
                p.Features,
                p.IsActive,
                p.CreatedAt,
                ActiveSubscriptions = p.Subscriptions.Count(s => s.Status == "Active" || s.Status == "Trial" || s.Status == "Trialing")
            })
            .ToListAsync();

        return Ok(new { success = true, data = plans });
    }

    /// <summary>
    /// Get plan by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var plan = await _db.ServicePlans
            .Include(p => p.Subscriptions)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
            return NotFound(new { success = false, message = "Không tìm thấy gói dịch vụ" });

        return Ok(new { success = true, data = new
        {
            plan.Id,
            plan.PlanName,
            plan.Price,
            plan.DurationDays,
            plan.Features,
            plan.IsActive,
            plan.CreatedAt,
            TotalSubscriptions = plan.Subscriptions.Count,
            ActiveSubscriptions = plan.Subscriptions.Count(s => s.Status == "Active" || s.Status == "Trial")
        }});
    }

    /// <summary>
    /// Create new plan
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlanRequest request)
    {
        if (await _db.ServicePlans.AnyAsync(p => p.PlanName == request.PlanName))
            return Conflict(new { success = false, message = $"Plan '{request.PlanName}' đã tồn tại" });

        var plan = new ServicePlan
        {
            Id = Guid.NewGuid(),
            PlanName = request.PlanName,
            Price = request.Price,
            DurationDays = request.DurationDays,
            Features = request.Features,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.ServicePlans.Add(plan);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = plan.Id }, new { success = true, data = plan });
    }

    /// <summary>
    /// Update plan (price, features, duration, active status)
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlanRequest request)
    {
        var plan = await _db.ServicePlans.FindAsync(id);
        if (plan == null)
            return NotFound(new { success = false, message = "Không tìm thấy gói dịch vụ" });

        if (!string.IsNullOrWhiteSpace(request.PlanName))
            plan.PlanName = request.PlanName;

        if (request.Price.HasValue)
            plan.Price = request.Price.Value;

        if (request.DurationDays.HasValue)
            plan.DurationDays = request.DurationDays.Value;

        if (request.Features != null)
            plan.Features = request.Features;

        if (request.IsActive.HasValue)
            plan.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();

        return Ok(new { success = true, data = plan, message = "Cập nhật gói dịch vụ thành công" });
    }

    /// <summary>
    /// Deactivate plan (soft delete — không xóa thật vì có subscriptions liên kết)
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var plan = await _db.ServicePlans
            .Include(p => p.Subscriptions)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
            return NotFound(new { success = false, message = "Không tìm thấy gói dịch vụ" });

        var activeCount = plan.Subscriptions.Count(s => s.Status == "Active" || s.Status == "Trial");
        if (activeCount > 0)
            return BadRequest(new { success = false, message = $"Không thể vô hiệu hóa — còn {activeCount} subscriptions đang active" });

        plan.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(new { success = true, message = "Đã vô hiệu hóa gói dịch vụ" });
    }
}

// --- Request DTOs ---

public class CreatePlanRequest
{
    public string PlanName { get; set; } = null!;
    public decimal Price { get; set; }
    public int DurationDays { get; set; } = 30;
    public string? Features { get; set; }
}

public class UpdatePlanRequest
{
    public string? PlanName { get; set; }
    public decimal? Price { get; set; }
    public int? DurationDays { get; set; }
    public string? Features { get; set; }
    public bool? IsActive { get; set; }
}
