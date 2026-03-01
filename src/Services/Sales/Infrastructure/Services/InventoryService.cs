using _360Retail.Services.Sales.Application.DTOs;
using _360Retail.Services.Sales.Application.Interfaces;
using _360Retail.Services.Sales.Domain.Entities;
using _360Retail.Services.Sales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace _360Retail.Services.Sales.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly SalesDbContext _db;

    public InventoryService(SalesDbContext db)
    {
        _db = db;
    }

    // Helper class for cross-schema queries
    private class IdWrapper { public Guid Id { get; set; } }

    public async Task<Guid> CreateTicketAsync(CreateInventoryTicketDto dto, Guid storeId, Guid userId)
    {
        // Validate type
        if (dto.Type != "Import" && dto.Type != "Export")
            throw new Exception("Loại phiếu phải là 'Import' hoặc 'Export'");

        if (dto.Items == null || dto.Items.Count == 0)
            throw new Exception("Phiếu kho phải có ít nhất một sản phẩm");

        // Resolve EmployeeId from UserId
        var employeeWrapper = await _db.Database
            .SqlQueryRaw<IdWrapper>(
                "SELECT id as \"Id\" FROM hr.employees WHERE app_user_id = {0} AND store_id = {1}",
                userId, storeId)
            .FirstOrDefaultAsync();

        // Validate all products belong to this store
        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Include(p => p.ProductVariants)
            .Where(p => productIds.Contains(p.Id) && p.StoreId == storeId)
            .ToListAsync();

        if (products.Count != productIds.Count)
            throw new Exception("Một số sản phẩm không tìm thấy hoặc không thuộc cửa hàng");

        // Build ticket
        var ticket = new InventoryTicket
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            Code = GenerateTicketCode(dto.Type),
            Type = dto.Type,
            Status = "Draft",
            Note = dto.Note,
            CreatedByEmployeeId = employeeWrapper?.Id,
            CreatedAt = DateTime.UtcNow,
            TotalQuantity = 0
        };

        int totalQty = 0;

        foreach (var itemDto in dto.Items)
        {
            if (itemDto.Quantity <= 0)
                throw new Exception("Số lượng phải lớn hơn 0");

            var product = products.First(p => p.Id == itemDto.ProductId);

            // Validate variant if provided
            if (itemDto.ProductVariantId.HasValue)
            {
                var variant = product.ProductVariants
                    .FirstOrDefault(v => v.Id == itemDto.ProductVariantId.Value);
                if (variant == null)
                    throw new Exception($"Không tìm thấy biến thể của sản phẩm '{product.ProductName}'");
            }

            var ticketItem = new InventoryTicketItem
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                ProductId = itemDto.ProductId,
                ProductVariantId = itemDto.ProductVariantId,
                Quantity = itemDto.Quantity,
                Note = itemDto.Note
            };

            ticket.Items.Add(ticketItem);
            totalQty += itemDto.Quantity;
        }

        ticket.TotalQuantity = totalQty;

        _db.InventoryTickets.Add(ticket);
        await _db.SaveChangesAsync();

        return ticket.Id;
    }

    public async Task ConfirmTicketAsync(Guid ticketId, Guid storeId, Guid userId)
    {
        var ticket = await _db.InventoryTickets
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.StoreId == storeId);

        if (ticket == null)
            throw new Exception("Không tìm thấy phiếu kho");

        if (ticket.Status != "Draft")
            throw new Exception($"Không thể xác nhận phiếu có trạng thái '{ticket.Status}'. Chỉ phiếu Nháp mới được xác nhận.");

        // Resolve confirmer EmployeeId
        var employeeWrapper = await _db.Database
            .SqlQueryRaw<IdWrapper>(
                "SELECT id as \"Id\" FROM hr.employees WHERE app_user_id = {0} AND store_id = {1}",
                userId, storeId)
            .FirstOrDefaultAsync();

        // Load products and variants to update stock
        var productIds = ticket.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Include(p => p.ProductVariants)
            .Where(p => productIds.Contains(p.Id) && p.StoreId == storeId)
            .ToListAsync();

        foreach (var item in ticket.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);

            if (item.ProductVariantId.HasValue)
            {
                // Update variant stock
                var variant = product.ProductVariants.First(v => v.Id == item.ProductVariantId.Value);

                if (ticket.Type == "Import")
                    variant.StockQuantity += item.Quantity;
                else // Export
                {
                    if (variant.StockQuantity < item.Quantity)
                        throw new Exception($"Không đủ tồn kho cho sản phẩm '{product.ProductName}' (Biến thể: {variant.Sku}). Còn lại: {variant.StockQuantity}");
                    variant.StockQuantity -= item.Quantity;
                }
            }
            else
            {
                // Update base product stock
                if (ticket.Type == "Import")
                    product.StockQuantity += item.Quantity;
                else // Export
                {
                    if (product.StockQuantity < item.Quantity)
                        throw new Exception($"Không đủ tồn kho cho sản phẩm '{product.ProductName}'. Còn lại: {product.StockQuantity}");
                    product.StockQuantity -= item.Quantity;
                }
            }
        }

        ticket.Status = "Confirmed";
        ticket.ConfirmedByEmployeeId = employeeWrapper?.Id;
        ticket.ConfirmedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task CancelTicketAsync(Guid ticketId, Guid storeId)
    {
        var ticket = await _db.InventoryTickets
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.StoreId == storeId);

        if (ticket == null)
            throw new Exception("Không tìm thấy phiếu kho");

        if (ticket.Status != "Draft")
            throw new Exception($"Không thể hủy phiếu có trạng thái '{ticket.Status}'. Chỉ phiếu Nháp mới được hủy.");

        ticket.Status = "Cancelled";
        await _db.SaveChangesAsync();
    }

    public async Task DeleteTicketAsync(Guid ticketId, Guid storeId)
    {
        var ticket = await _db.InventoryTickets
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.StoreId == storeId);

        if (ticket == null)
            throw new Exception("Không tìm thấy phiếu kho");

        if (ticket.Status == "Confirmed")
            throw new Exception("Không thể xóa phiếu đã xác nhận. Tồn kho đã được cập nhật.");

        ticket.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<PagedResult<InventoryTicketDto>> GetAllAsync(
        Guid storeId, string? type, string? status, int page, int pageSize)
    {
        var query = _db.InventoryTickets
            .Where(t => t.StoreId == storeId && !t.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrEmpty(type))
            query = query.Where(t => t.Type == type);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new InventoryTicketDto
            {
                Id = t.Id,
                Code = t.Code,
                Type = t.Type,
                Status = t.Status,
                TotalQuantity = t.TotalQuantity,
                Note = t.Note,
                CreatedByEmployeeId = t.CreatedByEmployeeId,
                ConfirmedByEmployeeId = t.ConfirmedByEmployeeId,
                CreatedAt = t.CreatedAt,
                ConfirmedAt = t.ConfirmedAt
            })
            .ToListAsync();

        return new PagedResult<InventoryTicketDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<InventoryTicketDto?> GetByIdAsync(Guid ticketId, Guid storeId)
    {
        var ticket = await _db.InventoryTickets
            .Include(t => t.Items)
                .ThenInclude(i => i.Product)
            .Include(t => t.Items)
                .ThenInclude(i => i.ProductVariant)
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.StoreId == storeId && !t.IsDeleted);

        if (ticket == null) return null;

        return new InventoryTicketDto
        {
            Id = ticket.Id,
            Code = ticket.Code,
            Type = ticket.Type,
            Status = ticket.Status,
            TotalQuantity = ticket.TotalQuantity,
            Note = ticket.Note,
            CreatedByEmployeeId = ticket.CreatedByEmployeeId,
            ConfirmedByEmployeeId = ticket.ConfirmedByEmployeeId,
            CreatedAt = ticket.CreatedAt,
            ConfirmedAt = ticket.ConfirmedAt,
            Items = ticket.Items.Select(i => new InventoryTicketItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.ProductName,
                ProductVariantId = i.ProductVariantId,
                Sku = i.ProductVariant?.Sku,
                Size = i.ProductVariant?.Size,
                Color = i.ProductVariant?.Color,
                Quantity = i.Quantity,
                Note = i.Note
            }).ToList()
        };
    }

    private string GenerateTicketCode(string type)
    {
        var prefix = type == "Import" ? "IMP" : "EXP";
        return $"{prefix}-{DateTime.UtcNow:yyMMdd}-{new Random().Next(1000, 9999)}";
    }
}
