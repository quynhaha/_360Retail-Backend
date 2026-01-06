using _360Retail.Services.Sales.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _360Retail.Services.Sales.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetAllAsync();
        Task<CategoryDto> CreateAsync(CreateCategoryDto request, Guid storeId);
        Task UpdateAsync(UpdateCategoryDto request);
        Task DeleteAsync(Guid id);
    }
}
