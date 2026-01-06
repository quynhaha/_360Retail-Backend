using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Sales.Application.DTOs
{
    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "Category name cannot be empty")]
        public string CategoryName { get; set; }
        public Guid? ParentId { get; set; } 
    }
}
