using AlgoRhythm.Dtos;

namespace AlgoRhythm.Services;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request);
    Task VerifyEmailAsync(VerifyEmailRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}