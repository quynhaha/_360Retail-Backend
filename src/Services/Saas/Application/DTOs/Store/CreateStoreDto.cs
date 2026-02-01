using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Saas.Application.DTOs.Stores;

public class CreateStoreDto
{
    [Required(ErrorMessage = "Tên cửa hàng là bắt buộc")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Tên cửa hàng phải từ 2-200 ký tự")]
    public string StoreName { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
    public string? Address { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? Phone { get; set; }
    
    /// <summary>
    /// Required for paid users to purchase subscription for new store
    /// </summary>
    public Guid? PlanId { get; set; }
}
