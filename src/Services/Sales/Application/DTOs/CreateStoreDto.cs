using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Sales.Application.DTOs
{
    public class CreateStoreDto
    {
        [Required]
        public string StoreName { get; set; }

        public string? Address { get; set; }

        public string? Phone { get; set; }

        public bool IsActive { get; set; } = true;
    }
}