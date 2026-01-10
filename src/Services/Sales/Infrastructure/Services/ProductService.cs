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
            // Parse variants từ JSON string
            var variantDtos = request.GetVariants();
            
            // DEBUG: Log số lượng variants nhận được từ request
            Console.WriteLine($"[DEBUG] CreateAsync - VariantsJson: {request.VariantsJson ?? "(null)"}");
            Console.WriteLine($"[DEBUG] CreateAsync - Parsed {variantDtos.Count} variants for product: {request.ProductName}");
            if (variantDtos.Count > 0)
            {
                foreach (var v in variantDtos)
                {
                    Console.WriteLine($"[DEBUG] Request Variant: SKU={v.Sku}, Size={v.Size}, Color={v.Color}, StockQty={v.StockQuantity}");
                }
            }

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
            
            // 3. Tạo Product entity
            var product = new Product
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                ProductName = request.ProductName,
                BarCode = request.BarCode,
                Price = request.Price,
                CostPrice = request.CostPrice,
                StockQuantity = request.StockQuantity,
                Description = request.Description,
                CategoryId = request.CategoryId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // 4. Upload ảnh (nếu có)
            if (request.ImageFile != null)
            {
                product.ImageUrl = await _storageService.SaveFileAsync(request.ImageFile, "products");
            }

            // 5. Tạo ProductVariants từ parsed DTOs
            foreach (var vDto in variantDtos)
            {
                var variant = new ProductVariant
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Sku = vDto.Sku,
                    Size = vDto.Size,
                    Color = vDto.Color,
                    PriceOverride = vDto.PriceOverride,
                    StockQuantity = vDto.StockQuantity
                };
                product.ProductVariants.Add(variant);
                Console.WriteLine($"[DEBUG] Created Variant: Id={variant.Id}, SKU={variant.Sku}, Size={variant.Size}, Color={variant.Color}");
            }

            Console.WriteLine($"[DEBUG] Total ProductVariants to save: {product.ProductVariants.Count}");

            // 6. Lưu DB
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            
            Console.WriteLine($"[DEBUG] Product saved successfully with Id: {product.Id}");
            return product.Id;
        }



      public async Task<PagedResult<ProductDto>> GetAllAsync(
    Guid storeId, 
    string? keyword, 
    Guid? categoryId,
    int page = 1,
    int pageSize = 20,
    bool includeInactive = false)
 {
     var query = _context.Products
         .Where(p => p.StoreId == storeId)
        .Where(p => includeInactive || p.IsActive)  // Filter inactive by default
         .Include(p => p.Category)
         .Include(p => p.ProductVariants)  // Include for TotalStock calculation
         .AsQueryable();
     // ... filtering logic ...
    
    var totalCount = await query.CountAsync();
   var items = await query
       .OrderByDescending(p => p.CreatedAt)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
       .ToListAsync();

    return new PagedResult<ProductDto>
    {
       Items = _mapper.Map<List<ProductDto>>(items),
        TotalCount = totalCount,
        PageNumber = page,
        PageSize = pageSize
    };
 }

        public async Task<ProductDto> GetByIdAsync(Guid id, Guid storeId)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariants)
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
product.BarCode = request.BarCode;
 product.Price = request.Price;
product.CostPrice = request.CostPrice;
product.StockQuantity = request.StockQuantity;
 product.Description = request.Description;
 product.CategoryId = request.CategoryId;
product.IsActive = request.IsActive;

            if (request.ImageFile != null)
            {
                if (!string.IsNullOrEmpty(product.ImageUrl))
                    await _storageService.DeleteFileAsync(product.ImageUrl);

                product.ImageUrl = await _storageService.SaveFileAsync(
                    request.ImageFile, "products");
            }

            // Handle Variants Update
            var variantDtos = request.GetVariants();
            if (variantDtos.Count > 0)
            {
                var existingVariants = await _context.ProductVariants
                    .Where(pv => pv.ProductId == product.Id).ToListAsync();

                foreach (var vDto in variantDtos)
                {
                    if (vDto.Id == null || vDto.Id == Guid.Empty)
                    {
                        // Add New
                        var newVariant = new ProductVariant
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            Sku = vDto.Sku,
                            Size = vDto.Size,
                            Color = vDto.Color,
                            PriceOverride = vDto.PriceOverride,
                            StockQuantity = vDto.StockQuantity
                        };
                        _context.ProductVariants.Add(newVariant);
                    }
                    else
                    {
                        // Update Existing or Delete
                        var existing = existingVariants.FirstOrDefault(x => x.Id == vDto.Id);
                        if (existing != null)
                        {
                            if (vDto.IsDeleted) 
                            {
                                _context.ProductVariants.Remove(existing);
                            }
                            else
                            {
                                existing.Sku = vDto.Sku;
                                existing.Size = vDto.Size;
                                existing.Color = vDto.Color;
                                existing.PriceOverride = vDto.PriceOverride;
                                existing.StockQuantity = vDto.StockQuantity;
                            }
                        }
                    }
                }
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
            // if (!string.IsNullOrEmpty(product.ImageUrl))
            //     await _storageService.DeleteFileAsync(product.ImageUrl);
            // _context.Products.Remove(product);
            product.IsActive = false;  // Soft delete
            await _context.SaveChangesAsync();
        }
    }
}