namespace _360Retail.Services.Identity.Application.DTOs;

public record InviteStaffDto(
    string Email,
    string RoleInStore // Staff | Manager
);