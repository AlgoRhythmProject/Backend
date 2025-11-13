using AlgoRhythm.Data;
using AlgoRhythm.Shared.Dtos;
using AlgoRhythm.Shared.Models.Users;
using AlgoRhythm.Interfaces;
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
    private readonly UserManager<User> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext db,
        UserManager<User> userManager,
        IEmailSender emailSender,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _db = db;
        _userManager = userManager;
        _emailSender = emailSender;
        _config = config;
        _logger = logger;
    }

    public async Task RegisterAsync(RegisterRequest request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains("@"))
            throw new ArgumentException("Invalid email format.");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new ArgumentException("Password must be at least 6 characters long.");

        if (string.IsNullOrWhiteSpace(request.FirstName))
            throw new ArgumentException("First name is required.");

        if (string.IsNullOrWhiteSpace(request.LastName))
            throw new ArgumentException("Last name is required.");

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
            throw new InvalidOperationException("Email already registered.");
        }

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = false
        };

        // Create user with password (Identity handles hashing)
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        // Add to default role
        await _userManager.AddToRoleAsync(user, "User");

        // Generate 6-digit verification code
        var simpleCode = Random.Shared.Next(100000, 999999).ToString();
        user.SecurityStamp = simpleCode; // Temporary storage
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User registered successfully: {Email}", user.Email);

        // Send verification email
        await SendVerificationEmailAsync(user, simpleCode);
    }

    private async Task SendVerificationEmailAsync(User user, string code)
    {
        var expiryTime = DateTime.UtcNow.AddHours(1).ToLocalTime();

        var subject = "Potwierdź swój adres email w AlgoRhythm";

        var plain = $@"Cześć {user.FirstName}!

Dziękujemy za rejestrację w AlgoRhythm - platformie do nauki programowania.

Aby dokończyć proces rejestracji, potwierdź swój adres email wpisując poniższy kod weryfikacyjny:

{code}

Kod jest ważny przez 1 godzinę (do {expiryTime:HH:mm, dd.MM.yyyy}).

Jeśli to nie Ty zakładałeś konto, zignoruj tę wiadomość.

Pozdrawiamy,
Zespół AlgoRhythm

---
To jest wiadomość automatyczna, prosimy na nią nie odpowiadać.";

        var html = $@"<p>Cześć <strong>{user.FirstName}</strong>!</p>

<p>Dziękujemy za rejestrację w AlgoRhythm - platformie do nauki programowania.</p>

<p>Aby dokończyć proces rejestracji, potwierdź swój adres email wpisując poniższy kod weryfikacyjny:</p>

<p style='font-size: 24px; font-weight: bold; letter-spacing: 2px;'>{code}</p>

<p>Kod jest ważny przez 1 godzinę (do {expiryTime:HH:mm, dd.MM.yyyy}).</p>

<p>Jeśli to nie Ty zakładałeś konto, zignoruj tę wiadomość.</p>

<p>Pozdrawiamy,<br>Zespół AlgoRhythm</p>

<hr>
<p style='font-size: 12px; color: #666;'>To jest wiadomość automatyczna, prosimy na nią nie odpowiadać.</p>";

        try
        {
            await _emailSender.SendEmailAsync(user.Email!, subject, plain, html);
            _logger.LogInformation("Verification email sent to: {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to: {Email}", user.Email);
        }
    }

    public async Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Email verification attempt for non-existent user: {Email}", request.Email);
            throw new InvalidOperationException("User not found.");
        }

        if (user.EmailConfirmed)
        {
            _logger.LogInformation("Email already verified for user: {Email}", user.Email);
            return await GenerateAuthResponseAsync(user);
        }

        // Verify code (stored in SecurityStamp temporarily)
        if (user.SecurityStamp != request.Code)
        {
            _logger.LogWarning("Invalid verification code for user: {Email}", user.Email);
            throw new InvalidOperationException("Invalid verification code.");
        }

        // Confirm email
        user.EmailConfirmed = true;
        user.SecurityStamp = Guid.NewGuid().ToString(); // Reset security stamp
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Email verified successfully for user: {Email}", user.Email);

        return await GenerateAuthResponseAsync(user);
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        // Create JWT
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
        var issuer = _config["Jwt:Issuer"] ?? "AlgoRhythm.Api";
        var audience = _config["Jwt:Audience"] ?? "AlgoRhythm.Client";
        var minutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
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

        var userDto = new UserDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.CreatedAt
        );

        return new AuthResponse(tokenString, expires, userDto);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Login attempt with invalid password for user: {Email}", user.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.EmailConfirmed)
        {
            _logger.LogWarning("Login attempt with unverified email: {Email}", user.Email);
            throw new UnauthorizedAccessException("Email not verified. Please verify your email first.");
        }

        _logger.LogInformation("User logged in successfully: {Email}", user.Email);

        return await GenerateAuthResponseAsync(user);
    }
}