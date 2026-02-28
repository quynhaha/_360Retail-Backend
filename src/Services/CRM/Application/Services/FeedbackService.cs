using _360Retail.Services.CRM.Application.DTOs;

namespace _360Retail.Services.CRM.Application.Services;

public interface IFeedbackService
{
    Task<FeedbackDto> CreateAsync(Guid storeId, CreateFeedbackDto dto, Guid? employeeId);
    Task<FeedbackDto> CreatePublicAsync(Guid orderId, CreatePublicFeedbackDto dto);
    Task<PagedResult<FeedbackDto>> GetByStoreAsync(Guid storeId, int? rating, DateTime? from, DateTime? to, int page, int pageSize);
    Task<PagedResult<FeedbackDto>> GetByCustomerAsync(Guid customerId, Guid storeId, int page, int pageSize);
    Task<FeedbackSummaryDto> GetSummaryAsync(Guid storeId);
}
