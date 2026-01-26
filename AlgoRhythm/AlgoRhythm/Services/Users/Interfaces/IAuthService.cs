using AlgoRhythm.Shared.Dtos.Users;

namespace AlgoRhythm.Services.Users.Interfaces;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request);
    Task ResendVerificationCodeAsync(ResendVerificationCodeDto request);
    Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> GoogleLoginAsync(GoogleAuthRequest request);
    Task<RefreshTokenResponseDto> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task RevokeTokenAsync(string refreshToken, string ipAddress);
    Task RequestPasswordResetAsync(ResetPasswordRequestDto request);
    Task ResetPasswordAsync(ResetPasswordDto request);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto request);
    Task<UserDto> UpdateUserProfileAsync(Guid userId, UpdateUserProfileDto request);
}