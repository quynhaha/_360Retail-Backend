using _360Retail.Services.HR.Application.DTOs;
using _360Retail.Services.HR.Domain.Entities;
using _360Retail.Services.HR.Infrastructure.Persistence;
using _360Retail.Services.HR.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HR.Tests;

/// <summary>
/// TimekeepingService tests — Note: CheckIn/CheckOut methods use SqlQueryRaw for GPS
/// which is not supported by InMemory DB. We test the query/logic methods 
/// (GetHistory, GetSummary, GetTodayStatus) by seeding data directly.
/// </summary>
public class TimekeepingServiceTests
{
    private readonly HrDbContext _db;
    private readonly TimekeepingService _service;
    private readonly Guid _storeId = Guid.NewGuid();
    private readonly Guid _appUserId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();

    public TimekeepingServiceTests()
    {
        var options = new DbContextOptionsBuilder<HrDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new HrDbContext(options);
        var logger = new Mock<ILogger<TimekeepingService>>();
        _service = new TimekeepingService(_db, logger.Object);

        // Seed employee
        _db.Employees.Add(new Employee
        {
            Id = _employeeId,
            AppUserId = _appUserId,
            StoreId = _storeId,
            FullName = "Test Employee",
            Position = "Staff",
            IsActive = true
        });
        _db.SaveChanges();
    }

    private void SeedTimekeepingRecord(DateTime checkIn, DateTime? checkOut = null, bool isLate = false)
    {
        _db.Timekeepings.Add(new Timekeeping
        {
            Id = Guid.NewGuid(),
            EmployeeId = _employeeId,
            StoreId = _storeId,
            CheckInTime = checkIn,
            CheckOutTime = checkOut,
            IsLate = isLate,
            LocationGps = "10.7769,106.7009"
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetHistory_WithRecords_ReturnsAll()
    {
        SeedTimekeepingRecord(DateTime.UtcNow.AddHours(-2), DateTime.UtcNow);

        var results = await _service.GetHistoryAsync(_storeId, null, null, null, 1, 10);

        Assert.Single(results);
        Assert.Equal("Test Employee", results[0].EmployeeName);
    }

    [Fact]
    public async Task GetHistory_FilterByEmployee_ReturnsFiltered()
    {
        SeedTimekeepingRecord(DateTime.UtcNow.AddHours(-2));

        // Add another employee & record
        var otherEmpId = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = otherEmpId, AppUserId = Guid.NewGuid(),
            StoreId = _storeId, FullName = "Other", Position = "Staff", IsActive = true
        });
        _db.Timekeepings.Add(new Timekeeping
        {
            Id = Guid.NewGuid(), EmployeeId = otherEmpId,
            StoreId = _storeId, CheckInTime = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var results = await _service.GetHistoryAsync(_storeId, _employeeId, null, null, 1, 10);

        Assert.Single(results);
        Assert.Equal(_employeeId, results[0].EmployeeId);
    }

    [Fact]
    public async Task GetHistory_EmptyStore_ReturnsEmpty()
    {
        var results = await _service.GetHistoryAsync(Guid.NewGuid(), null, null, null, 1, 10);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetHistory_Pagination_RespectsPageSize()
    {
        // Add 5 records
        for (int i = 0; i < 5; i++)
            SeedTimekeepingRecord(DateTime.UtcNow.AddDays(-i));

        var page1 = await _service.GetHistoryAsync(_storeId, null, null, null, 1, 2);
        var page2 = await _service.GetHistoryAsync(_storeId, null, null, null, 2, 2);

        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
    }

    [Fact]
    public async Task GetSummary_CalculatesCorrectly()
    {
        var now = DateTime.UtcNow;
        // Day 1: 8 hours, late
        SeedTimekeepingRecord(now.Date.AddHours(10), now.Date.AddHours(18), isLate: true);
        // Day 2: 7 hours, on time
        SeedTimekeepingRecord(now.Date.AddDays(-1).AddHours(8), now.Date.AddDays(-1).AddHours(15));

        var results = await _service.GetSummaryAsync(_storeId, now.Month, now.Year);

        Assert.Single(results);
        var summary = results[0];
        Assert.Equal("Test Employee", summary.EmployeeName);
        Assert.Equal(2, summary.TotalDays);
        Assert.Equal(1, summary.LateDays);
        Assert.True(summary.TotalHours > 0);
    }

    [Fact]
    public async Task GetSummary_EmptyMonth_ReturnsEmpty()
    {
        var results = await _service.GetSummaryAsync(_storeId, 1, 2020);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetSummary_MultipleEmployees_ReturnsSeparate()
    {
        var now = DateTime.UtcNow;
        SeedTimekeepingRecord(now.Date.AddHours(8), now.Date.AddHours(17));

        // Add another employee
        var otherEmpId = Guid.NewGuid();
        _db.Employees.Add(new Employee
        {
            Id = otherEmpId, AppUserId = Guid.NewGuid(),
            StoreId = _storeId, FullName = "Employee 2", Position = "Manager", IsActive = true
        });
        _db.Timekeepings.Add(new Timekeeping
        {
            Id = Guid.NewGuid(), EmployeeId = otherEmpId,
            StoreId = _storeId, CheckInTime = now.Date.AddHours(9),
            CheckOutTime = now.Date.AddHours(18)
        });
        await _db.SaveChangesAsync();

        var results = await _service.GetSummaryAsync(_storeId, now.Month, now.Year);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task CheckIn_EmployeeNotFound_ThrowsException()
    {
        // This test works because the exception is thrown BEFORE SqlQueryRaw
        var dto = new CheckInDto();
        var fakeUserId = Guid.NewGuid();

        await Assert.ThrowsAsync<Exception>(() =>
            _service.CheckInAsync(_storeId, fakeUserId, dto));
    }

    [Fact]
    public async Task CheckOut_NoEmployee_ThrowsException()
    {
        await Assert.ThrowsAsync<Exception>(() =>
            _service.CheckOutAsync(_storeId, Guid.NewGuid(), new CheckOutDto()));
    }
}
