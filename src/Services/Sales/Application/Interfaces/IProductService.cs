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
        Task<List<ProductDto>> GetAllAsync(Guid storeId, string? keyword, Guid? categoryId);
        Task<ProductDto> GetByIdAsync(Guid id, Guid storeId);
        Task<Guid> CreateAsync(CreateProductDto request, Guid storeId);
        Task UpdateAsync(UpdateProductDto request, Guid storeId);
        Task DeleteAsync(Guid id, Guid storeId);
    }
}
