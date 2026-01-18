using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace _360Retail.Services.Sales.Application.DTOs
{
    /// <summary>
    /// DTO for partial update - only non-null fields will be updated
    /// </summary>
    public class UpdateProductDto
    {
        [Required]
        public Guid Id { get; set; }
        
        /// <summary>
        /// Product name - null means keep existing value
        /// </summary>
        public string? ProductName { get; set; }

        /// <summary>
        /// Barcode - null means keep existing value
        /// </summary>
        public string? BarCode { get; set; }

        /// <summary>
        /// Description - null means keep existing value
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Selling price - null means keep existing value
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Cost price - null means keep existing value
        /// </summary>
        public decimal? CostPrice { get; set; }

        /// <summary>
        /// Stock quantity - null means keep existing value
        /// </summary>
        public int? StockQuantity { get; set; }

        /// <summary>
        /// Category ID - null means keep existing value
        /// </summary>
        public Guid? CategoryId { get; set; }

        /// <summary>
        /// Active status - null means keep existing value
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// New image file - null means keep existing image
        /// </summary>
        public IFormFile? ImageFile { get; set; }

        /// <summary>
        /// JSON string array of variants for update
        /// </summary>
        public string? VariantsJson { get; set; }

        /// <summary>
        /// Parsed list of variants from VariantsJson
        /// Handles both single object and array format
        /// </summary>
        public List<UpdateProductVariantDto> GetVariants()
        {
            if (string.IsNullOrWhiteSpace(VariantsJson))
                return new List<UpdateProductVariantDto>();

            try
            {
                var json = VariantsJson.Trim();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                // If input starts with '{', it's a single object - wrap in array
                if (json.StartsWith("{"))
                {
                    json = "[" + json + "]";
                }
                
                return JsonSerializer.Deserialize<List<UpdateProductVariantDto>>(json, options) 
                    ?? new List<UpdateProductVariantDto>();
            }
            catch
            {
                return new List<UpdateProductVariantDto>();
            }
        }
    }
}
