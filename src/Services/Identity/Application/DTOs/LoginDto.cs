using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Identity.Application.DTOs;

public record LoginDto(
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    string Email,

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    string Password
);
