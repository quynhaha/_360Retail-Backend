using _360Retail.Services.Sales.Application.DTOs;

namespace _360Retail.Services.Sales.Application.Interfaces;

public interface IInventoryService
{
    Task<Guid> CreateTicketAsync(CreateInventoryTicketDto dto, Guid storeId, Guid userId);
    Task ConfirmTicketAsync(Guid ticketId, Guid storeId, Guid userId);
    Task CancelTicketAsync(Guid ticketId, Guid storeId);
    Task DeleteTicketAsync(Guid ticketId, Guid storeId);
    Task<PagedResult<InventoryTicketDto>> GetAllAsync(Guid storeId, string? type, string? status, int page, int pageSize);
    Task<InventoryTicketDto?> GetByIdAsync(Guid ticketId, Guid storeId);
}
