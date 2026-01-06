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

        public async Task<List<CategoryDto>> GetAllAsync()
        {
            // Include Parent để lấy tên danh mục cha
            var categories = await _context.Categories
                                           .Include(c => c.Parent)
                                           .OrderByDescending(c => c.Id) 
                                           .ToListAsync();
            return _mapper.Map<List<CategoryDto>>(categories);
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryDto request , Guid storeId)
        {
            // 1. Kiểm tra trùng tên (Optional)
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

        public async Task UpdateAsync(UpdateCategoryDto request)
        {
            var category = await _context.Categories.FindAsync(request.Id);
            if (category == null) throw new Exception("Category not found!");

            // Check trùng tên nếu sửa tên 
            if (category.CategoryName != request.CategoryName)
            {
                bool exists = await _context.Categories.AnyAsync(c => c.CategoryName == request.CategoryName);
                if (exists) throw new Exception("Category name already exists!");
            }

            // Map dữ liệu 
            category.CategoryName = request.CategoryName;
            category.ParentId = request.ParentId;
            category.IsActive = request.IsActive;

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var category = await _context.Categories.FindAsync(id);
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
