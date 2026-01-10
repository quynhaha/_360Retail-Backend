using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using _360Retail.Services.Sales.Application.Common;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace _360Retail.Services.Sales.Application.DTOs
{
    public class UpdateProductDto : BaseProductDto
    {
        [Required]
        public Guid Id { get; set; }
        
        public bool IsActive { get; set; } = true;

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

