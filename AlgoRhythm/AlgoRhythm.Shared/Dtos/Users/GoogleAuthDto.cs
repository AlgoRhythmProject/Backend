namespace AlgoRhythm.Shared.Dtos.Users;

/// <summary>
/// Request DTO for Google authentication
/// </summary>
public record GoogleAuthRequest(
    string IdToken,
    string? FirstName = null,
    string? LastName = null
);

/// <summary>
/// Response containing Google user info extracted from token
/// </summary>
public record GoogleUserInfo(
    string Email,
    string? FirstName,
    string? LastName,
    string GoogleId,
    bool EmailVerified
);