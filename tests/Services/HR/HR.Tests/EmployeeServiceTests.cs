using _360Retail.Services.HR.Application.DTOs;
using _360Retail.Services.HR.Domain.Entities;
using _360Retail.Services.HR.Infrastructure.Persistence;
using _360Retail.Services.HR.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HR.Tests;

public class EmployeeServiceTests
{
    private readonly HrDbContext _db;
    private readonly EmployeeService _service;
    private readonly Guid _storeId = Guid.NewGuid();
    private readonly Guid _empId = Guid.NewGuid();
    private readonly Guid _appUserId = Guid.NewGuid();

    public EmployeeServiceTests()
    {
        var options = new DbContextOptionsBuilder<HrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new HrDbContext(options);
        var mockHttp = new Mock<IHttpClientFactory>();
        var logger = new Mock<ILogger<EmployeeService>>();
        _service = new EmployeeService(_db, mockHttp.Object, logger.Object);

        // Seed employee
        _db.Employees.Add(new Employee
        {
            Id = _empId,
            AppUserId = _appUserId,
            StoreId = _storeId,
            FullName = "John Doe",
            Position = "Staff",
            BaseSalary = 5000000,
            IsActive = true,
            JoinDate = DateTime.UtcNow.AddMonths(-3)
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task CreateEmployee_Success()
    {
        var dto = new CreateEmployeeDto
        {
            AppUserId = Guid.NewGuid(),
            StoreId = _storeId,
            Email = "newstaff@test.com",
            Role = "Staff"
        };

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("newstaff@test.com", result.FullName);
        Assert.Equal("Staff", result.Position);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetAllByStoreId_ReturnsEmployees()
    {
        var results = await _service.GetAllByStoreIdAsync(_storeId);

        Assert.Single(results);
        Assert.Equal("John Doe", results[0].FullName);
    }

    [Fact]
    public async Task GetAllByStoreId_ExcludesInactive()
    {
        // Add inactive employee
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(),
            AppUserId = Guid.NewGuid(),
            StoreId = _storeId,
            FullName = "Inactive User",
            Position = "Staff",
            IsActive = false
        });
        await _db.SaveChangesAsync();

        var results = await _service.GetAllByStoreIdAsync(_storeId, includeInactive: false);

        Assert.Single(results); // Only active employee
    }

    [Fact]
    public async Task GetAllByStoreId_IncludesInactive()
    {
        _db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(),
            AppUserId = Guid.NewGuid(),
            StoreId = _storeId,
            FullName = "Inactive User",
            Position = "Staff",
            IsActive = false
        });
        await _db.SaveChangesAsync();

        var results = await _service.GetAllByStoreIdAsync(_storeId, includeInactive: true);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetById_ExistingEmployee_ReturnsEmployee()
    {
        var result = await _service.GetByIdAsync(_empId, _storeId);

        Assert.NotNull(result);
        Assert.Equal("John Doe", result!.FullName);
        Assert.Equal(5000000, result.BaseSalary);
    }

    [Fact]
    public async Task GetById_NonExisting_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid(), _storeId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProfile_Success()
    {
        var dto = new UpdateEmployeeProfileDto
        {
            FullName = "John Updated"
        };

        var result = await _service.UpdateProfileAsync(_appUserId, _storeId, dto);

        Assert.True(result);
        var emp = await _db.Employees.FindAsync(_empId);
        Assert.Equal("John Updated", emp!.FullName);
    }

    [Fact]
    public async Task UpdateAvatar_Success()
    {
        var result = await _service.UpdateAvatarAsync(_appUserId, _storeId, "https://cdn.test.com/avatar.jpg");

        Assert.True(result);
        var emp = await _db.Employees.FindAsync(_empId);
        Assert.Equal("https://cdn.test.com/avatar.jpg", emp!.AvatarUrl);
    }
}
