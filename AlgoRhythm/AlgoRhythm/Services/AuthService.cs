using AlgoRhythm.Data;
using AlgoRhythm.Dtos;
using AlgoRhythm.Interfaces;
using AlgoRhythm.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AlgoRhythm.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthService(
        ApplicationDbContext db,
        IEmailSender emailSender,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _config = config;
        _logger = logger;
    }

    public async Task RegisterAsync(RegisterRequest request)
    {
        // Walidacja email
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains("@"))
        {
            throw new ArgumentException("Invalid email format.");
        }

        // Walidacja hasła (min 6 znaków, możesz dostosować)
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            throw new ArgumentException("Password must be at least 6 characters long.");
        }

        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (exists)
        {
            _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
            throw new InvalidOperationException("Email already registered.");
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(null!, request.Password),
            IsEmailConfirmed = false,
            Role = "User" // domyślna rola
        };

        // Generuj 6-cyfrowy kod weryfikacyjny
        var rng = Random.Shared;
        var code = rng.Next(100000, 999999).ToString();
        user.EmailVerificationCode = code;
        user.EmailVerificationExpiryUtc = DateTime.UtcNow.AddHours(1);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User registered successfully: {Email}", user.Email);

        // Dane do emaila
        var expiryTime = user.EmailVerificationExpiryUtc.Value;
        var expiryTimeLocal = expiryTime.ToLocalTime();

        // Wyślij email z kodem weryfikacyjnym
        var subject = "AlgoRhythm — Email verification";
        var plain = $"Your verification code: {code}";
        var html = $"<p>Your verification code: <strong>{code}</strong></p>" +
                   $"<p>This code will expire at: {expiryTimeLocal:yyyy-MM-dd HH:mm:ss}</p>";

        try
        {
            await _emailSender.SendEmailAsync(user.Email, subject, plain, html);
            _logger.LogInformation("Verification email sent to: {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to: {Email}", user.Email);
            // Nie rzucaj błędu — użytkownik został zarejestrowany, ale email się nie wysłał
            // Możesz dodać endpoint do ponownego wysłania kodu
        }
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            _logger.LogWarning("Email verification attempt for non-existent user: {Email}", request.Email);
            throw new InvalidOperationException("User not found.");
        }

        if (user.IsEmailConfirmed)
        {
            _logger.LogInformation("Email already verified for user: {Email}", user.Email);
            return; // Email już zweryfikowany
        }

        if (user.EmailVerificationCode == null ||
            user.EmailVerificationExpiryUtc == null ||
            user.EmailVerificationExpiryUtc < DateTime.UtcNow)
        {
            _logger.LogWarning("Expired verification code for user: {Email}", user.Email);
            throw new InvalidOperationException("Verification code expired. Please request a new one.");
        }

        if (!string.Equals(user.EmailVerificationCode, request.Code, StringComparison.Ordinal))
        {
            _logger.LogWarning("Invalid verification code for user: {Email}", user.Email);
            throw new InvalidOperationException("Invalid verification code.");
        }

        user.IsEmailConfirmed = true;
        user.EmailVerificationCode = null;
        user.EmailVerificationExpiryUtc = null;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Email verified successfully for user: {Email}", user.Email);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verify == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login attempt with invalid password for user: {Email}", user.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsEmailConfirmed)
        {
            _logger.LogWarning("Login attempt with unverified email: {Email}", user.Email);
            throw new UnauthorizedAccessException("Email not verified. Please verify your email first.");
        }

        // Tworzenie JWT
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
        var issuer = _config["Jwt:Issuer"] ?? "AlgoRhythm.Api";
        var audience = _config["Jwt:Audience"] ?? "AlgoRhythm.Client";
        var minutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        if (!string.IsNullOrEmpty(user.Role))
        {
            claims.Add(new Claim(ClaimTypes.Role, user.Role));
        }

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(minutes);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogInformation("User logged in successfully: {Email}", user.Email);

        return new AuthResponse(tokenString, expires);
    }
}