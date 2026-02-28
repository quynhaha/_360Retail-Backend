using _360Retail.Services.Saas.Domain.Entities;
using _360Retail.Services.Saas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Saas.API.Services;

// ===== DTOs =====

public class CreatePlanReviewDto
{
    [Required(ErrorMessage = "PlanId là bắt buộc")]
    public Guid PlanId { get; set; }

    [Required(ErrorMessage = "Rating là bắt buộc")]
    [Range(1, 5, ErrorMessage = "Rating phải từ 1 đến 5")]
    public int Rating { get; set; }

    [MaxLength(2000, ErrorMessage = "Nội dung tối đa 2000 ký tự")]
    public string? Content { get; set; }
}

public class PlanReviewDto
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public string? PlanName { get; set; }
    public Guid UserId { get; set; }
    public Guid StoreId { get; set; }
    public string? StoreName { get; set; }
    public int Rating { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PlanReviewSummaryDto
{
    public Guid PlanId { get; set; }
    public string? PlanName { get; set; }
    public double AvgRating { get; set; }
    public int TotalReviews { get; set; }
    public Dictionary<int, int> Distribution { get; set; } = new();
}

/// <summary>
/// Dashboard thống kê cho SuperAdmin
/// </summary>
public class AdminReviewDashboardDto
{
    public int TotalReviews { get; set; }
    public double OverallAvgRating { get; set; }
    public int ReviewsThisMonth { get; set; }
    public List<PlanReviewSummaryDto> PerPlanStats { get; set; } = new();
}

// ===== Interface =====

public interface IPlanReviewService
{
    Task<PlanReviewDto> CreateAsync(Guid userId, Guid storeId, CreatePlanReviewDto dto);
    Task<PlanReviewDto?> GetMyReviewAsync(Guid userId, Guid planId);
    Task<List<PlanReviewDto>> GetByPlanAsync(Guid planId, int page, int pageSize);
    Task<PlanReviewSummaryDto> GetSummaryAsync(Guid planId);
    Task<List<PlanReviewSummaryDto>> GetAllPlanSummariesAsync();

    // Admin
    Task<List<PlanReviewDto>> GetAllReviewsAsync(Guid? planId, int? rating, int page, int pageSize);
    Task<bool> DeleteAsync(Guid reviewId);
    Task<AdminReviewDashboardDto> GetAdminDashboardAsync();
}

// ===== Service =====

public class PlanReviewService : IPlanReviewService
{
    private readonly SaasDbContext _db;
    private readonly ILogger<PlanReviewService> _logger;

    public PlanReviewService(SaasDbContext db, ILogger<PlanReviewService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PlanReviewDto> CreateAsync(Guid userId, Guid storeId, CreatePlanReviewDto dto)
    {
        // Validate plan exists
        var plan = await _db.ServicePlans.FindAsync(dto.PlanId);
        if (plan == null)
            throw new Exception("Gói dịch vụ không tồn tại");

        // Check if user already has subscription for this plan
        var hasSub = await _db.Subscriptions
            .AnyAsync(s => s.StoreId == storeId && s.PlanId == dto.PlanId);
        if (!hasSub)
            throw new Exception("Bạn chưa mua gói này, không thể đánh giá");

        // Check duplicate review
        var existing = await _db.PlanReviews
            .FirstOrDefaultAsync(r => r.UserId == userId && r.PlanId == dto.PlanId);
        if (existing != null)
            throw new Exception("Bạn đã đánh giá gói này rồi. Mỗi gói chỉ được đánh giá 1 lần");

        var store = await _db.Stores.FindAsync(storeId);

        var review = new PlanReview
        {
            Id = Guid.NewGuid(),
            PlanId = dto.PlanId,
            UserId = userId,
            StoreId = storeId,
            Rating = dto.Rating,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        };

        _db.PlanReviews.Add(review);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Plan review created: PlanId={PlanId}, UserId={UserId}, Rating={Rating}",
            dto.PlanId, userId, dto.Rating);

        return new PlanReviewDto
        {
            Id = review.Id,
            PlanId = review.PlanId,
            PlanName = plan.PlanName,
            UserId = review.UserId,
            StoreId = review.StoreId,
            StoreName = store?.StoreName,
            Rating = review.Rating,
            Content = review.Content,
            CreatedAt = review.CreatedAt
        };
    }

    public async Task<PlanReviewDto?> GetMyReviewAsync(Guid userId, Guid planId)
    {
        var review = await _db.PlanReviews
            .Include(r => r.Plan)
            .Include(r => r.Store)
            .FirstOrDefaultAsync(r => r.UserId == userId && r.PlanId == planId);

        return review == null ? null : MapToDto(review);
    }

    public async Task<List<PlanReviewDto>> GetByPlanAsync(Guid planId, int page, int pageSize)
    {
        return await _db.PlanReviews
            .Include(r => r.Plan)
            .Include(r => r.Store)
            .Where(r => r.PlanId == planId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => MapToDto(r))
            .ToListAsync();
    }

    public async Task<PlanReviewSummaryDto> GetSummaryAsync(Guid planId)
    {
        var plan = await _db.ServicePlans.FindAsync(planId);
        var ratings = await _db.PlanReviews
            .Where(r => r.PlanId == planId)
            .Select(r => r.Rating)
            .ToListAsync();

        return BuildSummary(planId, plan?.PlanName, ratings);
    }

    public async Task<List<PlanReviewSummaryDto>> GetAllPlanSummariesAsync()
    {
        var plans = await _db.ServicePlans.Where(p => p.IsActive == true).ToListAsync();
        var allReviews = await _db.PlanReviews.ToListAsync();

        return plans.Select(p =>
        {
            var ratings = allReviews.Where(r => r.PlanId == p.Id).Select(r => r.Rating).ToList();
            return BuildSummary(p.Id, p.PlanName, ratings);
        }).ToList();
    }

    private static PlanReviewSummaryDto BuildSummary(Guid planId, string? planName, List<int> ratings)
    {
        var distribution = Enumerable.Range(1, 5)
            .ToDictionary(r => r, r => ratings.Count(x => x == r));

        return new PlanReviewSummaryDto
        {
            PlanId = planId,
            PlanName = planName,
            AvgRating = ratings.Any() ? Math.Round(ratings.Average(), 1) : 0,
            TotalReviews = ratings.Count,
            Distribution = distribution
        };
    }

    // ===== Admin Methods =====

    public async Task<List<PlanReviewDto>> GetAllReviewsAsync(Guid? planId, int? rating, int page, int pageSize)
    {
        var query = _db.PlanReviews
            .Include(r => r.Plan)
            .Include(r => r.Store)
            .AsQueryable();

        if (planId.HasValue)
            query = query.Where(r => r.PlanId == planId.Value);

        if (rating.HasValue)
            query = query.Where(r => r.Rating == rating.Value);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new PlanReviewDto
            {
                Id = r.Id,
                PlanId = r.PlanId,
                PlanName = r.Plan != null ? r.Plan.PlanName : null,
                UserId = r.UserId,
                StoreId = r.StoreId,
                StoreName = r.Store != null ? r.Store.StoreName : null,
                Rating = r.Rating,
                Content = r.Content,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(Guid reviewId)
    {
        var review = await _db.PlanReviews.FindAsync(reviewId);
        if (review == null) return false;

        _db.PlanReviews.Remove(review);
        await _db.SaveChangesAsync();

        _logger.LogWarning("SuperAdmin deleted review {ReviewId} for PlanId={PlanId}",
            reviewId, review.PlanId);

        return true;
    }

    public async Task<AdminReviewDashboardDto> GetAdminDashboardAsync()
    {
        var plans = await _db.ServicePlans.Where(p => p.IsActive == true).ToListAsync();
        var allReviews = await _db.PlanReviews.ToListAsync();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var perPlanStats = plans.Select(p =>
        {
            var ratings = allReviews.Where(r => r.PlanId == p.Id).Select(r => r.Rating).ToList();
            return BuildSummary(p.Id, p.PlanName, ratings);
        }).ToList();

        return new AdminReviewDashboardDto
        {
            TotalReviews = allReviews.Count,
            OverallAvgRating = allReviews.Any() ? Math.Round(allReviews.Average(r => r.Rating), 1) : 0,
            ReviewsThisMonth = allReviews.Count(r => r.CreatedAt >= monthStart),
            PerPlanStats = perPlanStats
        };
    }

    private static PlanReviewDto MapToDto(PlanReview r) => new()
    {
        Id = r.Id,
        PlanId = r.PlanId,
        PlanName = r.Plan?.PlanName,
        UserId = r.UserId,
        StoreId = r.StoreId,
        StoreName = r.Store?.StoreName,
        Rating = r.Rating,
        Content = r.Content,
        CreatedAt = r.CreatedAt
    };
}
