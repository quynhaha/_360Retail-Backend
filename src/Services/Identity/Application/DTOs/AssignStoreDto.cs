
using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Identity.Application.DTOs
{
    public class AssignStoreDto
    {
        [Required]
        public Guid StoreId { get; set; }
        public string RoleInStore { get; set; } = "StoreOwner";
        public bool IsDefault { get; set; } = true;
    }
}
