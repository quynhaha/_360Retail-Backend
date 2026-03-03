using _360Retail.Services.CRM.Application.DTOs;
using _360Retail.Services.CRM.Application.Interfaces;
using _360Retail.Services.CRM.Domain.Entities;

namespace _360Retail.Services.CRM.Application.Services;

public interface ICustomerService
{
    Task<CustomerDto> CreateAsync(Guid storeId, CreateCustomerDto dto);
    Task<CustomerDto?> GetByIdAsync(Guid customerId, Guid storeId);
    Task<PagedResult<CustomerDto>> GetByStoreAsync(Guid storeId, int page, int pageSize);
    Task<CustomerDto?> UpdateAsync(Guid customerId, Guid storeId, UpdateCustomerDto dto);
    Task<bool> DeleteAsync(Guid customerId, Guid storeId);
}

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepo;

    public CustomerService(ICustomerRepository customerRepo)
    {
        _customerRepo = customerRepo;
    }

    public async Task<CustomerDto> CreateAsync(Guid storeId, CreateCustomerDto dto)
    {
        // Check duplicate phone within same store
        var existing = await _customerRepo.GetByPhoneAndStoreAsync(dto.PhoneNumber, storeId);
        if (existing != null)
            throw new InvalidOperationException("Customer with this phone number already exists in this store");

        var customer = new Customer
        {
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            ZaloId = dto.ZaloId,
            StoreId = storeId,
            TotalPoints = 0,
            Rank = "Bronze"
        };

        await _customerRepo.AddAsync(customer);
        return MapToDto(customer);
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid customerId, Guid storeId)
    {
        var customer = await _customerRepo.GetByIdAsync(customerId);
        if (customer == null || customer.StoreId != storeId) return null;
        return MapToDto(customer);
    }

    public async Task<PagedResult<CustomerDto>> GetByStoreAsync(Guid storeId, int page, int pageSize)
    {
        var customers = await _customerRepo.GetByStoreIdAsync(storeId, page, pageSize);
        var total = await _customerRepo.GetTotalCountByStoreAsync(storeId);
        var dtos = customers.Select(MapToDto);
        return new PagedResult<CustomerDto>(dtos, page, pageSize, total);
    }

    public async Task<CustomerDto?> UpdateAsync(Guid customerId, Guid storeId, UpdateCustomerDto dto)
    {
        var customer = await _customerRepo.GetByIdAsync(customerId);
        if (customer == null || customer.StoreId != storeId) return null;

        // Check phone uniqueness (exclude self)
        var existing = await _customerRepo.GetByPhoneAndStoreAsync(dto.PhoneNumber, storeId);
        if (existing != null && existing.Id != customerId)
            throw new InvalidOperationException("Another customer with this phone number already exists");

        customer.FullName = dto.FullName;
        customer.PhoneNumber = dto.PhoneNumber;
        customer.ZaloId = dto.ZaloId;

        await _customerRepo.UpdateAsync(customer);
        return MapToDto(customer);
    }

    public async Task<bool> DeleteAsync(Guid customerId, Guid storeId)
    {
        var customer = await _customerRepo.GetByIdAsync(customerId);
        if (customer == null || customer.StoreId != storeId) return false;

        await _customerRepo.DeleteAsync(customer);
        return true;
    }

    private static CustomerDto MapToDto(Customer c) => new()
    {
        Id = c.Id,
        FullName = c.FullName,
        PhoneNumber = c.PhoneNumber,
        ZaloId = c.ZaloId,
        LastPurchaseDate = c.LastPurchaseDate,
        TotalPoints = c.TotalPoints ?? 0,
        Rank = c.Rank,
        StoreId = c.StoreId
    };
}
