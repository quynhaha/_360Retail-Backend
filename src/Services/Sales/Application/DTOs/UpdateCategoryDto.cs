using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Sales.Application.DTOs
{
    public class UpdateCategoryDto
    {
        [Required]
        public Guid Id { get; set; } 

        [Required]
        public string CategoryName { get; set; }

        public Guid? ParentId { get; set; }

        public bool IsActive { get; set; } 
    }
}