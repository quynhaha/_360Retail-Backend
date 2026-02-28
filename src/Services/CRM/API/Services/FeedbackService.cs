using _360Retail.Services.CRM.Application.DTOs;
using _360Retail.Services.CRM.Application.Services;
using _360Retail.Services.CRM.Domain.Entities;
using _360Retail.Services.CRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace _360Retail.Services.CRM.API.Services;

public class FeedbackService : IFeedbackService
{
    private readonly CrmDbContext _db;
    private readonly ILogger<FeedbackService> _logger;

    public FeedbackService(CrmDbContext db, ILogger<FeedbackService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Public feedback via QR Code — linked to orderId, no auth required
    /// </summary>
    public async Task<FeedbackDto> CreatePublicAsync(Guid orderId, CreatePublicFeedbackDto dto)
    {
        // 1. Validate order exists (cross-service via raw SQL since orders are in Sales DB)
        // For now, we trust the orderId and use storeId/customerId from the DTO link
        // In production, this would call Sales API to verify order

        // 2. Check if this order already has feedback
        var existing = await _db.CustomerFeedbacks
            .FirstOrDefaultAsync(f => f.OrderId == orderId);
        if (existing != null)
            throw new Exception("Đơn hàng này đã được đánh giá rồi");

        // 3. Validate customer exists
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == dto.CustomerId && c.StoreId == dto.StoreId);
        if (customer == null)
            throw new Exception("Thông tin khách hàng không hợp lệ");

        var feedback = new CustomerFeedback
        {
            Id = Guid.NewGuid(),
            StoreId = dto.StoreId,
            CustomerId = dto.CustomerId,
            OrderId = orderId,
            Content = dto.Content,
            Rating = dto.Rating,
            Source = "QRCode",
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
        };

        _db.CustomerFeedbacks.Add(feedback);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Public feedback created via QR: OrderId={OrderId}, Rating={Rating}",
            orderId, dto.Rating);

        return MapToDto(feedback, customer.FullName);
    }

    public async Task<FeedbackDto> CreateAsync(Guid storeId, CreateFeedbackDto dto, Guid? employeeId)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == dto.CustomerId && c.StoreId == storeId);

        if (customer == null)
            throw new Exception("Khách hàng không tồn tại trong cửa hàng này");

        var feedback = new CustomerFeedback
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            CustomerId = dto.CustomerId,
            Content = dto.Content,
            Rating = dto.Rating,
            Source = dto.Source ?? "InStore",
            CreatedByEmployeeId = employeeId,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
        };

        _db.CustomerFeedbacks.Add(feedback);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Feedback created: CustomerId={CustomerId}, Rating={Rating}, Source={Source}",
            dto.CustomerId, dto.Rating, feedback.Source);

        return MapToDto(feedback, customer.FullName);
    }

    public async Task<PagedResult<FeedbackDto>> GetByStoreAsync(
        Guid storeId, int? rating, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var query = _db.CustomerFeedbacks
            .Include(f => f.Customer)
            .Where(f => f.StoreId == storeId);

        if (rating.HasValue)
            query = query.Where(f => f.Rating == rating.Value);

        if (from.HasValue)
            query = query.Where(f => f.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(f => f.CreatedAt <= to.Value.Date.AddDays(1));

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(f => MapToDto(f, f.Customer?.FullName)).ToList();
        return new PagedResult<FeedbackDto>(dtos, page, pageSize, total);
    }

    public async Task<PagedResult<FeedbackDto>> GetByCustomerAsync(
        Guid customerId, Guid storeId, int page, int pageSize)
    {
        var query = _db.CustomerFeedbacks
            .Include(f => f.Customer)
            .Where(f => f.CustomerId == customerId && f.StoreId == storeId);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(f => MapToDto(f, f.Customer?.FullName)).ToList();
        return new PagedResult<FeedbackDto>(dtos, page, pageSize, total);
    }

    public async Task<FeedbackSummaryDto> GetSummaryAsync(Guid storeId)
    {
        var feedbacks = await _db.CustomerFeedbacks
            .Where(f => f.StoreId == storeId && f.Rating.HasValue)
            .Select(f => f.Rating!.Value)
            .ToListAsync();

        if (!feedbacks.Any())
        {
            return new FeedbackSummaryDto
            {
                AvgRating = 0,
                TotalCount = 0,
                Distribution = new Dictionary<int, int>
                {
                    { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }
                }
            };
        }

        var distribution = Enumerable.Range(1, 5)
            .ToDictionary(r => r, r => feedbacks.Count(f => f == r));

        return new FeedbackSummaryDto
        {
            AvgRating = Math.Round(feedbacks.Average(), 1),
            TotalCount = feedbacks.Count,
            Distribution = distribution
        };
    }

    private static FeedbackDto MapToDto(CustomerFeedback f, string? customerName)
    {
        return new FeedbackDto
        {
            Id = f.Id,
            CustomerId = f.CustomerId,
            CustomerName = customerName,
            Content = f.Content,
            Rating = f.Rating ?? 0,
            Source = f.Source,
            CreatedByEmployeeId = f.CreatedByEmployeeId,
            CreatedAt = f.CreatedAt
        };
    }
}
