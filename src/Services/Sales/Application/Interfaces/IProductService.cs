using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _360Retail.Services.Sales.Application.DTOs;

namespace _360Retail.Services.Sales.Application.Interfaces
{
    public interface IProductService
    {
        Task<PagedResult<ProductDto>> GetAllAsync(
    Guid storeId, 
   string? keyword, 
   Guid? categoryId,
   int page = 1,
int pageSize = 20,
   bool includeInactive = false);
        Task<ProductDto> GetByIdAsync(Guid id, Guid storeId);
        Task<Guid> CreateAsync(CreateProductDto request, Guid storeId);
        Task UpdateAsync(UpdateProductDto request, Guid storeId);
        Task DeleteAsync(Guid id, Guid storeId);
    }
}
