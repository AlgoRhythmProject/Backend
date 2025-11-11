using AlgoRhythm.Data;
using AlgoRhythm.Dtos;
using AlgoRhythm.Interfaces;
using AlgoRhythm.Models.Users;
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
        // Walidacja
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains("@"))
            throw new ArgumentException("Invalid email format.");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new ArgumentException("Password must be at least 6 characters long.");

        if (string.IsNullOrWhiteSpace(request.FirstName))
            throw new ArgumentException("First name is required.");

        if (string.IsNullOrWhiteSpace(request.LastName))
            throw new ArgumentException("Last name is required.");

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
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsEmailConfirmed = false
        };

        // Generuj kod weryfikacyjny
        var rng = Random.Shared;
        var code = rng.Next(100000, 999999).ToString();
        user.EmailVerificationCode = code;
        user.EmailVerificationExpiryUtc = DateTime.UtcNow.AddHours(1);

        // Dodaj domyślną rolę "User"
        var defaultRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (defaultRole != null)
        {
            user.Roles.Add(defaultRole);
        }
        else
        {
            _logger.LogWarning("Default role 'User' not found in database");
        }

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User registered successfully: {Email}", user.Email);

        // Dane do emaila
        var expiryTime = user.EmailVerificationExpiryUtc.Value;
        var expiryTimeLocal = expiryTime.ToLocalTime();

        // Wyślij email z kodem weryfikacyjnym
        var subject = "Potwierdź swój adres email w AlgoRhythm";

        var plain = $@"Cześć {user.FirstName}!

            Dziękujemy za rejestrację w AlgoRhythm - platformie do nauki programowania.

            Aby dokończyć proces rejestracji, potwierdź swój adres email wpisując poniższy kod weryfikacyjny:

            {code}

            Kod jest ważny przez 1 godzinę (do {expiryTimeLocal:HH:mm, dd.MM.yyyy}).

            Jeśli to nie Ty zakładałeś konto, zignoruj tę wiadomość.

            Pozdrawiamy,
            Zespół AlgoRhythm

            ---
            To jest wiadomość automatyczna, prosimy na nią nie odpowiadać.";

                    var html = $@"<p>Cześć <strong>{user.FirstName}</strong>!</p>

            <p>Dziękujemy za rejestrację w AlgoRhythm - platformie do nauki programowania.</p>

            <p>Aby dokończyć proces rejestracji, potwierdź swój adres email wpisując poniższy kod weryfikacyjny:</p>

            <p style='font-size: 24px; font-weight: bold; letter-spacing: 2px;'>{code}</p>

            <p>Kod jest ważny do {expiryTimeLocal:HH:mm, dd.MM.yyyy}.</p>

            <p>Jeśli to nie Ty zakładałeś konto, zignoruj tę wiadomość.</p>

            <p>Pozdrawiamy,<br>Zespół AlgoRhythm</p>

            <hr>
            <p style='font-size: 12px; color: #666;'>To jest wiadomość automatyczna, prosimy na nią nie odpowiadać.</p>";

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

    public async Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Roles) // Załaduj role dla tokena
            .FirstOrDefaultAsync(u => u.Email == request.Email);
        
        if (user == null)
        {
            _logger.LogWarning("Email verification attempt for non-existent user: {Email}", request.Email);
            throw new InvalidOperationException("User not found.");
        }

        if (user.IsEmailConfirmed)
        {
            _logger.LogInformation("Email already verified for user: {Email}", user.Email);
            
            // Email już zweryfikowany - zaloguj użytknika
            return await GenerateAuthResponseAsync(user);
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

        // Automatyczne logowanie po weryfikacji
        return await GenerateAuthResponseAsync(user);
    }

    // Nowa metoda pomocnicza do generowania AuthResponse
    private async Task<AuthResponse> GenerateAuthResponseAsync(User user)
    {
        // Upewnij się że role są załadowane
        if (!user.Roles.Any())
        {
            await _db.Entry(user).Collection(u => u.Roles).LoadAsync();
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

        // Dodaj wszystkie role użytkownika jako claim'y
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));
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

        // Utwórz DTO użytkownika
        var userDto = new UserDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.CreatedAt
        );

        return new AuthResponse(tokenString, expires, userDto);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Roles) // Załaduj role użytkownika
            .FirstOrDefaultAsync(u => u.Email == request.Email);
        
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

        _logger.LogInformation("User logged in successfully: {Email}", user.Email);

        return await GenerateAuthResponseAsync(user);
    }
}