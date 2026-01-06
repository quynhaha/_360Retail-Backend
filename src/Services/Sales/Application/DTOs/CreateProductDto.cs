using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Sales.Application.DTOs
{
    public class CreateProductDto
    {
        [Required(ErrorMessage = "Product name is required")]
        public string ProductName { get; set; }

        public string? BarCode { get; set; }
        public string? Description { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "The selling price must be greater than 0")]
        public decimal Price { get; set; }

        public decimal? CostPrice { get; set; }
        public int StockQuantity { get; set; } = 0;

        [Required(ErrorMessage = "Please select a category")]
        public Guid CategoryId { get; set; }

        [Required(ErrorMessage = "Please select a store")]

        public IFormFile? ImageFile { get; set; }
    }
}