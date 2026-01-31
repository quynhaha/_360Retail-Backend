namespace _360Retail.Services.Identity.Application.DTOs;

/// <summary>
/// DTO for external OAuth login (Google, Facebook)
/// </summary>
public record ExternalLoginDto(
    /// <summary>
    /// The OAuth provider name ("Google" or "Facebook")
    /// </summary>
    string Provider,
    
    /// <summary>
    /// The ID token or access token from the OAuth provider
    /// </summary>
    string IdToken
);

/// <summary>
/// Response after successful external login
/// </summary>
public record ExternalAuthResultDto(
    string AccessToken,
    DateTime ExpiresAt,
    bool IsNewUser,
    string? Email,
    string? ProfilePictureUrl
);
