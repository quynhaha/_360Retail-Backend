using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Identity.Application.DTOs.SuperAdmin;

public class CreateUserDto
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8-100 ký tự")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Role là bắt buộc")]
    public string RoleName { get; set; } = null!;
}
