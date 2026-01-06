namespace _360Retail.Services.Identity.Application.DTOs;

public record ChangePasswordDto(
    string CurrentPassword,
    string NewPassword
);
