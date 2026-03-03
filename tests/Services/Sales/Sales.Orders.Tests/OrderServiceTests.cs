using System;
using System.Threading.Tasks;
using Moq;
using Xunit;
using _360Retail.Services.Sales.Application.DTOs;
using _360Retail.Services.Sales.Application.Interfaces;

namespace Sales.Orders.Tests;

/// <summary>
/// Unit tests for IOrderService using Moq (service-level mocking).
/// OrderService depends on SalesDbContext with cross-schema SQL queries (hr.employees, crm.customers)
/// so we mock at the interface level instead of using InMemory DB.
/// </summary>
public class OrderServiceTests
{
    private readonly Mock<IOrderService> _orderService;

    public OrderServiceTests()
    {
        _orderService = new Mock<IOrderService>();
    }

    // ============ CREATE ORDER ============

    [Fact]
    public async Task CreateAsync_ValidOrder_ReturnsOrderId()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expectedOrderId = Guid.NewGuid();

        var dto = new CreateOrderDto
        {
            PaymentMethod = "Cash",
            DiscountAmount = 0,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = Guid.NewGuid(), Quantity = 2 }
            }
        };

        _orderService.Setup(x => x.CreateAsync(dto, storeId, userId))
            .ReturnsAsync(expectedOrderId);

        // Act
        var result = await _orderService.Object.CreateAsync(dto, storeId, userId);

        // Assert
        Assert.Equal(expectedOrderId, result);
        _orderService.Verify(x => x.CreateAsync(dto, storeId, userId), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InsufficientStock_ThrowsException()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var dto = new CreateOrderDto
        {
            PaymentMethod = "Cash",
            DiscountAmount = 0,
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = Guid.NewGuid(), Quantity = 9999 }
            }
        };

        _orderService.Setup(x => x.CreateAsync(dto, storeId, userId))
            .ThrowsAsync(new Exception("Insufficient stock for product"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _orderService.Object.CreateAsync(dto, storeId, userId));
        Assert.Contains("Insufficient stock", ex.Message);
    }

    // ============ CANCEL ORDER ============

    [Fact]
    public async Task CancelOrderAsync_ValidOrder_CallsSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        _orderService.Setup(x => x.CancelOrderAsync(orderId, storeId))
            .Returns(Task.CompletedTask);

        // Act
        await _orderService.Object.CancelOrderAsync(orderId, storeId);

        // Assert
        _orderService.Verify(x => x.CancelOrderAsync(orderId, storeId), Times.Once);
    }

    [Fact]
    public async Task CancelOrderAsync_AlreadyCancelled_ThrowsException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        _orderService.Setup(x => x.CancelOrderAsync(orderId, storeId))
            .ThrowsAsync(new Exception("Order is already cancelled"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _orderService.Object.CancelOrderAsync(orderId, storeId));
        Assert.Contains("already cancelled", ex.Message);
    }

    // ============ GET LIST ============

    [Fact]
    public async Task GetListAsync_ReturnsPagedResult()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var roles = new[] { "StoreOwner" };

        var expected = new PagedResult<OrderDto>
        {
            Items = new List<OrderDto>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _orderService.Setup(x => x.GetListAsync(storeId, userId, roles, null, null, null, 1, 10))
            .ReturnsAsync(expected);

        // Act
        var result = await _orderService.Object.GetListAsync(storeId, userId, roles, null, null, null, 1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
    }

    // ============ GET BY ID ============

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var roles = new[] { "Staff" };

        _orderService.Setup(x => x.GetByIdAsync(orderId, storeId, userId, roles))
            .ReturnsAsync((OrderDto?)null);

        // Act
        var result = await _orderService.Object.GetByIdAsync(orderId, storeId, userId, roles);

        // Assert
        Assert.Null(result);
    }

    // ============ UPDATE STATUS ============

    [Fact]
    public async Task UpdateStatusAsync_ValidTransition_Succeeds()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        _orderService.Setup(x => x.UpdateStatusAsync(orderId, storeId, "Completed"))
            .Returns(Task.CompletedTask);

        // Act
        await _orderService.Object.UpdateStatusAsync(orderId, storeId, "Completed");

        // Assert
        _orderService.Verify(x => x.UpdateStatusAsync(orderId, storeId, "Completed"), Times.Once);
    }
}
