using AlgoRhythm.Shared.Dtos;

namespace AlgoRhythm.Services.Interfaces;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request);
    Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}