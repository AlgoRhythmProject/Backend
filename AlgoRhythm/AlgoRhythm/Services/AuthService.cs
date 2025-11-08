using AlgoRhythm.Data;
using AlgoRhythm.Dtos;
using AlgoRhythm.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthService(ApplicationDbContext db, IEmailSender emailSender, IConfiguration config)
    {
        _db = db;
        _emailSender = emailSender;
        _config = config;
    }

    public async Task RegisterAsync(RegisterRequest request)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (exists) throw new InvalidOperationException("Email already registered.");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(null!, request.Password),
            IsEmailConfirmed = false
        };

        // generate 6-digit verification code
        var rng = Random.Shared;
        var code = rng.Next(100000, 999999).ToString();
        user.EmailVerificationCode = code;
        user.EmailVerificationExpiryUtc = DateTime.UtcNow.AddHours(1);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var subject = "AlgoRhythm — Email verification";
        var plain = $"Your verification code: {code}";
        var html = $"<p>Your verification code: <strong>{code}</strong></p>" +
                   $"<p>It will expire in 1 hour.</p>";

        await _emailSender.SendEmailAsync(user.Email, subject, plain, html);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) throw new InvalidOperationException("User not found.");
        if (user.IsEmailConfirmed) return;

        if (user.EmailVerificationCode == null ||
            user.EmailVerificationExpiryUtc == null ||
            user.EmailVerificationExpiryUtc < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Verification code expired. Please request a new one.");
        }

        if (!string.Equals(user.EmailVerificationCode, request.Code, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid verification code.");
        }

        user.IsEmailConfirmed = true;
        user.EmailVerificationCode = null;
        user.EmailVerificationExpiryUtc = null;
        await _db.SaveChangesAsync();
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) throw new InvalidOperationException("Invalid credentials.");

        var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verify == PasswordVerificationResult.Failed) throw new InvalidOperationException("Invalid credentials.");

        if (!user.IsEmailConfirmed) throw new InvalidOperationException("Email not verified.");

        // create JWT
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
        var issuer = _config["Jwt:Issuer"] ?? "AlgoRhythm";
        var audience = _config["Jwt:Audience"] ?? "AlgoRhythmClient";
        var minutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60");

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };
        if (!string.IsNullOrEmpty(user.Role))
            claims.Add(new Claim(ClaimTypes.Role, user.Role));

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(minutes);
        var token = new JwtSecurityToken(issuer: issuer, audience: audience, claims: claims, expires: expires, signingCredentials: creds);
        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponse(tokenString, expires);
    }
}