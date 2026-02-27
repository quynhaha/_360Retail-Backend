using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Identity.Application.DTOs;

public class InviteUserDto
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "StoreId là bắt buộc")]
    public Guid StoreId { get; set; }

    [Required(ErrorMessage = "Role là bắt buộc")]
    [RegularExpression("^(Staff|Manager)$", ErrorMessage = "Role phải là Staff hoặc Manager")]
    public string Role { get; set; } = "Staff";
}
