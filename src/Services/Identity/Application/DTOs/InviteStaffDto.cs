using System.ComponentModel.DataAnnotations;

namespace _360Retail.Services.Identity.Application.DTOs;

public record InviteStaffDto(
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    string Email,

    [Required(ErrorMessage = "Role là bắt buộc")]
    [RegularExpression("^(Staff|Manager)$", ErrorMessage = "Role phải là Staff hoặc Manager")]
    string RoleInStore
);