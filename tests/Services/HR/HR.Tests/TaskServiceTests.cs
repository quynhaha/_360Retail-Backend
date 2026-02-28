using _360Retail.Services.HR.Application.DTOs;
using _360Retail.Services.HR.Application.Interfaces;
using _360Retail.Services.HR.Domain.Entities;
using _360Retail.Services.HR.Infrastructure.Persistence;
using _360Retail.Services.HR.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace HR.Tests;

public class TaskServiceTests
{
    private readonly HrDbContext _db;
    private readonly TaskService _service;
    private readonly Guid _storeId = Guid.NewGuid();
    private readonly Guid _ownerAppUserId = Guid.NewGuid();
    private readonly Guid _staffEmployeeId = Guid.NewGuid();

    public TaskServiceTests()
    {
        var options = new DbContextOptionsBuilder<HrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new HrDbContext(options);
        var mockEmail = new Mock<IEmailService>();
        var mockHttp = new Mock<IHttpClientFactory>();
        _service = new TaskService(_db, mockEmail.Object, mockHttp.Object);

        // Seed staff employee
        _db.Employees.Add(new Employee
        {
            Id = _staffEmployeeId,
            AppUserId = Guid.NewGuid(),
            StoreId = _storeId,
            FullName = "Staff User",
            Position = "Staff",
            IsActive = true
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task CreateTask_AsOwner_Success()
    {
        var dto = new CreateTaskDto
        {
            Title = "Clean the store",
            AssigneeId = _staffEmployeeId,
            Priority = "High",
            Description = "Daily cleaning"
        };

        var result = await _service.CreateAsync(dto, _storeId, _ownerAppUserId, new[] { "StoreOwner" });

        Assert.NotNull(result);
        Assert.Equal("Clean the store", result.Title);
        Assert.Equal("High", result.Priority);
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task GetAllByStore_ReturnsTasks()
    {
        // Seed a task
        _db.Tasks.Add(new WorkTask
        {
            Id = Guid.NewGuid(),
            StoreId = _storeId,
            AssigneeId = _staffEmployeeId,
            Title = "Test Task",
            Status = "Pending",
            IsActive = true
        });
        await _db.SaveChangesAsync();

        var results = await _service.GetAllByStoreAsync(_storeId);

        Assert.Single(results);
        Assert.Equal("Test Task", results[0].Title);
    }

    [Fact]
    public async Task GetById_ExistingTask_ReturnsTask()
    {
        var taskId = Guid.NewGuid();
        _db.Tasks.Add(new WorkTask
        {
            Id = taskId,
            StoreId = _storeId,
            AssigneeId = _staffEmployeeId,
            Title = "Specific Task",
            Status = "InProgress",
            IsActive = true
        });
        await _db.SaveChangesAsync();

        var result = await _service.GetByIdAsync(taskId, _storeId);

        Assert.NotNull(result);
        Assert.Equal("Specific Task", result!.Title);
    }

    [Fact]
    public async Task GetById_NonExisting_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid(), _storeId);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteTask_SoftDeletes()
    {
        var taskId = Guid.NewGuid();
        _db.Tasks.Add(new WorkTask
        {
            Id = taskId,
            StoreId = _storeId,
            AssigneeId = _staffEmployeeId,
            Title = "To Delete",
            Status = "Pending",
            IsActive = true
        });
        await _db.SaveChangesAsync();

        var deleted = await _service.DeleteAsync(taskId, _storeId);

        Assert.True(deleted);
        var task = await _db.Tasks.FindAsync(taskId);
        Assert.False(task!.IsActive);
    }

    [Fact]
    public async Task UpdateStatus_ByOwner_Success()
    {
        var taskId = Guid.NewGuid();
        _db.Tasks.Add(new WorkTask
        {
            Id = taskId,
            StoreId = _storeId,
            AssigneeId = _staffEmployeeId,
            Title = "Status Task",
            Status = "Pending",
            IsActive = true
        });
        await _db.SaveChangesAsync();

        var updated = await _service.UpdateStatusAsync(
            taskId, _storeId, _ownerAppUserId, new[] { "StoreOwner" }, "Completed");

        Assert.True(updated);
        var task = await _db.Tasks.FindAsync(taskId);
        Assert.Equal("Completed", task!.Status);
    }
}
