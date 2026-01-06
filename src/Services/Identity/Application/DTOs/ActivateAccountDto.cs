namespace _360Retail.Services.Identity.Application.DTOs;

public record ActivateAccountDto(
    string Token,
    string Password
);
