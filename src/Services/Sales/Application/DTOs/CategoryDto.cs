using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _360Retail.Services.Sales.Application.DTOs
{
    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string CategoryName { get; set; }
        public Guid? ParentId { get; set; } 
        public string ParentName { get; set; }
    }
}