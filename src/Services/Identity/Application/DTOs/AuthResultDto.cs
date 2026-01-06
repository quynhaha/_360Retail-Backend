namespace _360Retail.Services.Identity.Application.DTOs;

public record AuthResultDto(
    string AccessToken,
    DateTime ExpiresAt
);