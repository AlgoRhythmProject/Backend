namespace AlgoRhythm.Shared.Dtos.Users;

/// <summary>
/// Request for refreshing JWT access token.
/// </summary>
public record RefreshTokenRequestDto(string RefreshToken);

/// <summary>
/// Response containing new access token and refresh token.
/// </summary>
public record RefreshTokenResponseDto(
    string AccessToken,
    DateTime AccessTokenExpiresUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresUtc
);