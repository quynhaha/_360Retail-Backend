using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Sales.Application.DTOs
{
    public class UpdateProductDto : CreateProductDto
    {
        [Required]
        public Guid Id { get; set; }
         public bool IsActive { get; set; } = true;
        // Ghi đè (new) để cho phép null (nếu user không muốn thay ảnh thì không gửi trường này)
        public new IFormFile? ImageFile { get; set; }
    }
}
