using AlgoRhythm.Shared.Dtos.Users;

namespace AlgoRhythm.Services.Users.Interfaces;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request);
    Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task ResendVerificationCodeAsync(ResendVerificationCodeDto request);
    Task RequestPasswordResetAsync(ResetPasswordRequestDto request);
    Task ResetPasswordAsync(ResetPasswordDto request);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto request);
}