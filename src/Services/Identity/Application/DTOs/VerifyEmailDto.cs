using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Identity.Application.DTOs;

public record VerifyEmailDto(
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    string Email,

    [Required(ErrorMessage = "Mã OTP là bắt buộc")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải đúng 6 chữ số")]
    string OtpCode
);

public record ResendOtpDto(
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    string Email
);
