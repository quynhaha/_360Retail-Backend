namespace _360Retail.Services.Identity.Application.DTOs;

public record InviteStaffEmailModel(
    string ToEmail,
    string StoreName,
    string InviteLink
);
