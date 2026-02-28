using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Identity.Application.DTOs;

public record ChangePasswordDto(
    [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
    string CurrentPassword,

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu mới phải từ 8-100 ký tự")]
    string NewPassword
);
