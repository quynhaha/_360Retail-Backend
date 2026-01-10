using _360Retail.Services.Sales.Application.DTOs;

namespace _360Retail.Services.Sales.Application.Interfaces;

public interface IOrderService
{
    Task<Guid> CreateAsync(CreateOrderDto dto, Guid storeId, Guid userId);
    Task<PagedResult<OrderDto>> GetListAsync(Guid storeId, string? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<OrderDto?> GetByIdAsync(Guid id, Guid storeId);
    Task UpdateStatusAsync(Guid id, Guid storeId, string status);
}
