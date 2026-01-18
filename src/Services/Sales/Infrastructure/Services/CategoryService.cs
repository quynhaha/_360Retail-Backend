using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using _360Retail.Services.Sales.Application.DTOs;
using _360Retail.Services.Sales.Application.Interfaces;
using _360Retail.Services.Sales.Domain.Entities;
using _360Retail.Services.Sales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace _360Retail.Services.Sales.Infrastructure.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly SalesDbContext _context;
        private readonly IMapper _mapper;

        public CategoryService(SalesDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<CategoryDto>> GetAllAsync(Guid storeId)
        {
            // Filter by StoreId - only return categories belonging to the current store
            var categories = await _context.Categories
                                           .Where(c => c.StoreId == storeId && c.IsActive)
                                           .Include(c => c.Parent)
                                           .OrderByDescending(c => c.Id) 
                                           .ToListAsync();
            return _mapper.Map<List<CategoryDto>>(categories);
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryDto request , Guid storeId)
        {
            // 1. Kiểm tra trùng tên trong cùng store
            bool exists = await _context.Categories.AnyAsync(c => c.StoreId == storeId
                && c.CategoryName == request.CategoryName);
            if (exists) throw new Exception("Category name already exists!");

            // 2. Map và Lưu
            var category = _mapper.Map<Category>(request);
            category.Id = Guid.NewGuid();
            category.StoreId = storeId;

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return _mapper.Map<CategoryDto>(category);
        }

        public async Task UpdateAsync(UpdateCategoryDto request, Guid storeId)
        {
            // Validate store ownership
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.Id && c.StoreId == storeId);
            if (category == null) throw new Exception("Category not found!");

            // PARTIAL UPDATE: Only update fields that are provided (not null)
            
            // Update CategoryName if provided
            if (request.CategoryName != null)
            {
                // Check trùng tên nếu sửa tên (trong cùng store)
                if (category.CategoryName != request.CategoryName)
                {
                    bool exists = await _context.Categories.AnyAsync(c => 
                        c.StoreId == storeId && 
                        c.CategoryName == request.CategoryName);
                    if (exists) throw new Exception("Category name already exists!");
                }
                category.CategoryName = request.CategoryName;
            }

            // Update ParentId if provided
            if (request.ParentId.HasValue)
            {
                // Use Guid.Empty to remove parent (set to null)
                category.ParentId = request.ParentId.Value == Guid.Empty ? null : request.ParentId.Value;
            }

            // Update IsActive if provided
            if (request.IsActive.HasValue)
            {
                category.IsActive = request.IsActive.Value;
            }

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id, Guid storeId)
        {
            // Validate store ownership
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.StoreId == storeId);
            if (category == null) throw new Exception("Category not found!");

            //  Không được xóa nếu đang có danh mục con
            bool hasChildren = await _context.Categories.AnyAsync(c => c.ParentId == id);
            if (hasChildren) throw new Exception("Subcategories must be deleted first!");

            //  Không được xóa nếu đang có sản phẩm (Optional)
            bool hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts) throw new Exception("The category contains the product, it cannot be deleted!");

            // soft delete
            category.IsActive = false;
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }
    }
}

