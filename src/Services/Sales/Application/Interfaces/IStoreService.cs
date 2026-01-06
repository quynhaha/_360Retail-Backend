using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _360Retail.Services.Sales.Application.DTOs;

namespace _360Retail.Services.Sales.Application.Interfaces
{
    public interface IStoreService
    {
        Task<Guid> CreateAsync(CreateStoreDto request);
        Task<List<StoreDto>> GetAllAsync();
        
    }
}