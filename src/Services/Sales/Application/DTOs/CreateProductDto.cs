using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using _360Retail.Services.Sales.Application.Common;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace _360Retail.Services.Sales.Application.DTOs
{
    public abstract class BaseProductDto
    {
        [Required(ErrorMessage = "Product name is required")]
        public string ProductName { get; set; } = null!;

        public string? BarCode { get; set; }
        public string? Description { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "The selling price must be greater than 0")]
        public decimal Price { get; set; }

        public decimal? CostPrice { get; set; }
        public int? StockQuantity { get; set; } = 0;

        [Required(ErrorMessage = "Please select a category")]
        public Guid CategoryId { get; set; }
    }

    public class CreateProductDto : BaseProductDto
    {
        public IFormFile? ImageFile { get; set; }

        /// <summary>
        /// JSON string array of variants, e.g: [{"sku":"SKU001","size":"M","color":"Red","priceOverride":150000,"stockQuantity":10}]
        /// </summary>
        public string? VariantsJson { get; set; }

        /// <summary>
        /// Parsed list of variants from VariantsJson
        /// Handles both single object and array format
        /// </summary>
        public List<CreateProductVariantDto> GetVariants()
        {
            if (string.IsNullOrWhiteSpace(VariantsJson))
                return new List<CreateProductVariantDto>();

            try
            {
                var json = VariantsJson.Trim();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                // If input starts with '{', it's a single object - wrap in array
                if (json.StartsWith("{"))
                {
                    json = "[" + json + "]";
                }
                
                return JsonSerializer.Deserialize<List<CreateProductVariantDto>>(json, options) 
                    ?? new List<CreateProductVariantDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] GetVariants parse error: {ex.Message}");
                return new List<CreateProductVariantDto>();
            }
        }
    }
}

