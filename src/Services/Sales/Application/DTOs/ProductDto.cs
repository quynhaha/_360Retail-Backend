using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _360Retail.Services.Sales.Application.DTOs
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; }
        public string? BarCode { get; set; }
        public decimal Price { get; set; }
        public decimal? CostPrice { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; } 
        public string? Description { get; set; }

        public Guid? CategoryId { get; set; }
        public string CategoryName { get; set; } 

        // Stock Management
        public int TotalStock { get; set; }
        public bool HasVariants { get; set; }
        public bool IsInStock { get; set; }
        public bool IsActive { get; set; }

        public List<ProductVariantDto> Variants { get; set; } = new();
    }
}
