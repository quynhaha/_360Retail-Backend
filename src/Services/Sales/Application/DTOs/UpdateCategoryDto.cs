using System;
using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Sales.Application.DTOs
{
    /// <summary>
    /// DTO for partial update - only non-null fields will be updated
    /// </summary>
    public class UpdateCategoryDto
    {
        [Required]
        public Guid Id { get; set; } 

        /// <summary>
        /// Category name - null means keep existing value
        /// </summary>
        public string? CategoryName { get; set; }

        /// <summary>
        /// Parent category ID - null means keep existing, use Guid.Empty to remove parent
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// Active status - null means keep existing value
        /// </summary>
        public bool? IsActive { get; set; } 
    }
}