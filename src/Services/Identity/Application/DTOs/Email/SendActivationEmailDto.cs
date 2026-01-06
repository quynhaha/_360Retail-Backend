namespace _360Retail.Services.Identity.Application.DTOs;

public record SendActivationEmailDto(
    string ToEmail,
    string ActivationLink
);
