namespace AlgoRhythm.Shared.Dtos.Users;

public record RegisterRequest(string Email, string Password, string FirstName, string LastName);
public record VerifyEmailRequest(string Email, string Code);
public record LoginRequest(string Email, string Password);

public record AuthResponse(
    string Token,
    DateTime ExpiresUtc,
    UserDto User
);