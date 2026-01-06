using _360Retail.Services.Sales.Application.DTOs;
using _360Retail.Services.Sales.Application.Interfaces;
using _360Retail.Services.Sales.Domain.Entities;
using _360Retail.Services.Sales.Infrastructure.Persistence;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace _360Retail.Services.Sales.Infrastructure.Services
{
    public class ProductService : IProductService
    {
        private readonly SalesDbContext _context;
        private readonly IMapper _mapper;
        private readonly IStorageService _storageService;

        public ProductService(SalesDbContext context, IMapper mapper, IStorageService storageService)
        {
            _context = context;
            _mapper = mapper;
            _storageService = storageService;
        }

        public async Task<Guid> CreateAsync(CreateProductDto request, Guid storeId)
        {
            // 1. Check Category thuộc Store
            var categoryExists = await _context.Categories.AnyAsync(c =>
                c.Id == request.CategoryId &&
                c.StoreId == storeId);
            if (!categoryExists)
                throw new Exception("Category does not belong to this store");
            // 2. Check trùng Product trong Store
            bool productExists = await _context.Products.AnyAsync(p =>
                p.StoreId == storeId &&
                p.ProductName == request.ProductName);
            if (productExists)
                throw new Exception("Product already exists in this store");
            // 3. Map dữ liệu
            var product = _mapper.Map<Product>(request);

            // Gán các giá trị hệ thống
            product.Id = Guid.NewGuid();
            product.StoreId = storeId;
            product.IsActive = true;
            product.CreatedAt = DateTime.UtcNow;
            // 4. Upload ảnh (nếu có)
            if (request.ImageFile != null)
            {
                product.ImageUrl = await _storageService.SaveFileAsync(request.ImageFile, "products");
            }
            // 5. Lưu DB
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product.Id;
        }



        public async Task<List<ProductDto>> GetAllAsync(Guid storeId, string? keyword, Guid? categoryId)
        {
            var query = _context.Products
                .Where(p => p.StoreId == storeId)
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(keyword.ToLower()));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            var list = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<ProductDto>>(list);
        }

        public async Task<ProductDto> GetByIdAsync(Guid id, Guid storeId)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id && p.StoreId == storeId);
            if (product == null)
                throw new Exception("Product not found");
            return _mapper.Map<ProductDto>(product);
        }

        public async Task UpdateAsync(UpdateProductDto request, Guid storeId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p =>
                p.Id == request.Id &&
                p.StoreId == storeId);

            if (product == null)
                throw new Exception("Product not found");

            bool categoryExists = await _context.Categories.AnyAsync(c =>
                c.Id == request.CategoryId &&
                c.StoreId == storeId);

            if (!categoryExists)
                throw new Exception("Category does not belong to this store");

            product.ProductName = request.ProductName;
            product.Price = request.Price;
            product.Description = request.Description;
            product.CategoryId = request.CategoryId;

            if (request.ImageFile != null)
            {
                if (!string.IsNullOrEmpty(product.ImageUrl))
                    await _storageService.DeleteFileAsync(product.ImageUrl);

                product.ImageUrl = await _storageService.SaveFileAsync(
                    request.ImageFile, "products");
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id, Guid storeId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.StoreId == storeId);

            if (product == null)
                throw new Exception("Product not found");
            if (!string.IsNullOrEmpty(product.ImageUrl))
                await _storageService.DeleteFileAsync(product.ImageUrl);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }
}