using AlgoRhythm.Shared.Dtos.Users;

namespace AlgoRhythm.Services.Users.Interfaces;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request);
    Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task RequestPasswordResetAsync(ResetPasswordRequestDto request);
    Task ResetPasswordAsync(ResetPasswordDto request);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto request);
    Task ResendVerificationCodeAsync(ResendVerificationCodeDto request);
    Task<UserDto> UpdateUserProfileAsync(Guid userId, UpdateUserProfileDto request);
}