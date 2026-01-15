using AlgoRhythm.Data;
using AlgoRhythm.Services.Achievements.Interfaces;
using AlgoRhythm.Services.Courses.Interfaces;
using AlgoRhythm.Services.Users.Exceptions;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Dtos.Users;
using AlgoRhythm.Shared.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AlgoRhythm.Services.Users;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;
    private readonly ICourseProgressService _courseProgressService;
    private readonly IAchievementService _achievementService;
    private readonly ApplicationDbContext _context;
    private readonly IUserStreakService _streakService;

    // Simple in-memory rate limiting
    private static readonly Dictionary<string, DateTime> _lastEmailSent = new();
    private static readonly TimeSpan _emailCooldown = TimeSpan.FromMinutes(1);

    public AuthService(
        UserManager<User> userManager,
        IEmailSender emailSender,
        IConfiguration config,
        ILogger<AuthService> logger,
        ICourseProgressService courseProgressService,
        IAchievementService achievementService,
        ApplicationDbContext context,
        IUserStreakService streakService)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _config = config;
        _logger = logger;
        _courseProgressService = courseProgressService;
        _achievementService = achievementService;
        _context = context;
        _streakService = streakService;
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
            throw new EmailAlreadyExistsException();
        }

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = false,
            CurrentStreak = 0,
            LongestStreak = 0,
            LastLoginDate = null
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

        // Initialize course progress for all courses
        try
        {
            await _courseProgressService.InitializeAllCoursesForUserAsync(user.Id, CancellationToken.None);
            _logger.LogInformation("Initialized course progress for new user: {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize course progress for user: {UserId}", user.Id);
            // Don't throw - user was created successfully
        }

        // Initialize achievements for all achievements
        try
        {
            await _achievementService.InitializeAchievementsForUserAsync(user.Id, CancellationToken.None);
            _logger.LogInformation("Initialized achievements for new user: {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize achievements for user: {UserId}", user.Id);
            // Don't throw - user was created successfully
        }

        // Generate and send verification code
        await GenerateAndSendVerificationCodeAsync(user);

        _logger.LogInformation("User registered successfully: {Email}", user.Email);
    }

    public async Task ResendVerificationCodeAsync(ResendVerificationCodeDto request)
    {
        // Rate limiting check
        if (_lastEmailSent.TryGetValue(request.Email, out var lastSent))
        {
            var timeSinceLastEmail = DateTime.UtcNow - lastSent;
            if (timeSinceLastEmail < _emailCooldown)
            {
                var waitTime = _emailCooldown - timeSinceLastEmail;
                throw new TooManyRequestsException(
                    $"Please wait {waitTime.Seconds} seconds before requesting another code.");
            }
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Resend verification code attempt for non-existent user: {Email}", request.Email);
            throw new UserNotFoundException();
        }

        if (user.EmailConfirmed)
        {
            _logger.LogWarning("Resend verification code attempt for already verified user: {Email}", request.Email);
            throw new EmailAlreadyVerifiedException();
        }

        // Generate and send new verification code
        await GenerateAndSendVerificationCodeAsync(user);

        _logger.LogInformation("Verification code resent to: {Email}", user.Email);
    }

    public async Task RequestPasswordResetAsync(ResetPasswordRequestDto request)
    {
        // Rate limiting check
        if (_lastEmailSent.TryGetValue(request.Email, out var lastSent))
        {
            var timeSinceLastEmail = DateTime.UtcNow - lastSent;
            if (timeSinceLastEmail < _emailCooldown)
            {
                var waitTime = _emailCooldown - timeSinceLastEmail;
                throw new TooManyRequestsException(
                    $"Please wait {waitTime.Seconds} seconds before requesting another code.");
            }
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Password reset request for non-existent user: {Email}", request.Email);
            return;
        }

        if (!user.EmailConfirmed)
        {
            _logger.LogWarning("Password reset request for unverified user: {Email}", request.Email);
            throw new EmailNotVerifiedException();
        }

        // Generate and send password reset code
        await GenerateAndSendPasswordResetCodeAsync(user);

        _logger.LogInformation("Password reset code sent to: {Email}", user.Email);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            throw new ArgumentException("Password must be at least 6 characters long.");

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Password reset attempt for non-existent user: {Email}", request.Email);
            throw new UserNotFoundException();
        }

        // Verify code (stored in SecurityStamp temporarily)
        if (user.SecurityStamp != request.Code)
        {
            _logger.LogWarning("Invalid password reset code for user: {Email}", user.Email);
            throw new InvalidVerificationCodeException();
        }

        // Remove current password and set new one
        var removePasswordResult = await _userManager.RemovePasswordAsync(user);
        if (!removePasswordResult.Succeeded)
        {
            var errors = string.Join(", ", removePasswordResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to reset password: {errors}");
        }

        var addPasswordResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
        if (!addPasswordResult.Succeeded)
        {
            var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to set new password: {errors}");
        }

        // Reset security stamp and revoke all refresh tokens
        user.SecurityStamp = Guid.NewGuid().ToString();
        await _userManager.UpdateAsync(user);
        await RevokeAllUserTokensAsync(user.Id, "Password reset");

        _logger.LogInformation("Password reset successfully for user: {Email}", user.Email);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            throw new ArgumentException("Password must be at least 6 characters long.");

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("Password change attempt for non-existent user: {UserId}", userId);
            throw new UserNotFoundException();
        }

        // Verify current password
        var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
        if (!isCurrentPasswordValid)
        {
            _logger.LogWarning("Password change attempt with invalid current password for user: {UserId}", userId);
            throw new InvalidPasswordException();
        }

        // Change password
        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to change password: {errors}");
        }

        // Revoke all refresh tokens after password change
        await RevokeAllUserTokensAsync(userId, "Password changed");

        _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
    }

    private async Task GenerateAndSendVerificationCodeAsync(User user)
    {
        // Generate 6-digit verification code
        var code = Random.Shared.Next(100000, 999999).ToString();
        user.SecurityStamp = code; // Temporary storage
        await _userManager.UpdateAsync(user);

        // Send email
        await SendVerificationEmailAsync(user, code);

        // Update rate limiting
        _lastEmailSent[user.Email!] = DateTime.UtcNow;
    }

    private async Task GenerateAndSendPasswordResetCodeAsync(User user)
    {
        // Generate 6-digit reset code
        var code = Random.Shared.Next(100000, 999999).ToString();
        user.SecurityStamp = code; // Temporary storage
        await _userManager.UpdateAsync(user);

        // Send email
        await SendPasswordResetEmailAsync(user, code);

        // Update rate limiting
        _lastEmailSent[user.Email!] = DateTime.UtcNow;
    }

    private async Task SendVerificationEmailAsync(User user, string code)
    {
        var expiryTime = DateTime.UtcNow.AddHours(1).ToLocalTime();

        var subject = "Verify your email address at AlgoRhythm";

        var plain = $@"Hello {user.FirstName}!

Thank you for registering at AlgoRhythm - a programming learning platform.

To complete the registration process, please verify your email address by entering the verification code below:

{code}

The code is valid for 1 hour (until {expiryTime:HH:mm, dd.MM.yyyy}).

If you did not create this account, please ignore this message.

Best regards,
AlgoRhythm Team

---
This is an automated message, please do not reply.";

        var html = $@"<p>Hello <strong>{user.FirstName}</strong>!</p>

<p>Thank you for registering at AlgoRhythm - a programming learning platform.</p>

<p>To complete the registration process, please verify your email address by entering the verification code below:</p>

<p style='font-size: 24px; font-weight: bold; letter-spacing: 2px;'>{code}</p>

<p>The code is valid for 1 hour (until {expiryTime:HH:mm, dd.MM.yyyy}).</p>

<p>If you did not create this account, please ignore this message.</p>

<p>Best regards,<br>AlgoRhythm Team</p>

<hr>
<p style='font-size: 12px; color: #666;'>This is an automated message, please do not reply.</p>";

        try
        {
            await _emailSender.SendEmailAsync(user.Email!, subject, plain, html);
            _logger.LogInformation("Verification email sent to: {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to: {Email}", user.Email);
            throw;
        }
    }

    private async Task SendPasswordResetEmailAsync(User user, string code)
    {
        var expiryTime = DateTime.UtcNow.AddHours(1).ToLocalTime();

        var subject = "Reset your password at AlgoRhythm";

        var plain = $@"Hello {user.FirstName}!

We received a request to reset your password for your AlgoRhythm account.

To reset your password, use the following verification code:

{code}

The code is valid for 1 hour (until {expiryTime:HH:mm, dd.MM.yyyy}).

If you did not request a password reset, please ignore this message and your password will remain unchanged.

Best regards,
AlgoRhythm Team

---
This is an automated message, please do not reply.";

        var html = $@"<p>Hello <strong>{user.FirstName}</strong>!</p>

<p>We received a request to reset your password for your AlgoRhythm account.</p>

<p>To reset your password, use the following verification code:</p>

<p style='font-size: 24px; font-weight: bold; letter-spacing: 2px;'>{code}</p>

<p>The code is valid for 1 hour (until {expiryTime:HH:mm, dd.MM.yyyy}).</p>

<p>If you did not request a password reset, please ignore this message and your password will remain unchanged.</p>

<p>Best regards,<br>AlgoRhythm Team</p>

<hr>
<p style='font-size: 12px; color: #666;'>This is an automated message, please do not reply.</p>";

        try
        {
            await _emailSender.SendEmailAsync(user.Email!, subject, plain, html);
            _logger.LogInformation("Password reset email sent to: {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to: {Email}", user.Email);
            throw;
        }
    }

    public async Task<AuthResponse> VerifyEmailAsync(VerifyEmailRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Email verification attempt for non-existent user: {Email}", request.Email);
            throw new UserNotFoundException();
        }

        if (user.EmailConfirmed)
        {
            _logger.LogInformation("Email already verified for user: {Email}", user.Email);
            return await GenerateAuthResponseAsync(user, "unknown");
        }

        // Verify code (stored in SecurityStamp temporarily)
        if (user.SecurityStamp != request.Code)
        {
            _logger.LogWarning("Invalid verification code for user: {Email}", user.Email);
            throw new InvalidVerificationCodeException();
        }

        // Confirm email
        user.EmailConfirmed = true;
        user.SecurityStamp = Guid.NewGuid().ToString(); // Reset security stamp
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Email verified successfully for user: {Email}", user.Email);

        // Update streak on first verification (counts as first login)
        try
        {
            await _streakService.UpdateLoginStreakAsync(user.Id);
            _logger.LogInformation("Login streak initialized for newly verified user: {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize login streak for user {UserId}", user.Id);
        }

        return await GenerateAuthResponseAsync(user, "unknown");
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user, string ipAddress)
    {
        var roles = await _userManager.GetRolesAsync(user);

        // Create JWT Access Token
        var key = _config["Jwt:Key"] 
            ?? Environment.GetEnvironmentVariable("JWT_KEY") 
            ?? throw new InvalidOperationException("JWT key is not configured.");
        
        var issuer = _config["Jwt:Issuer"] ?? "AlgoRhythm.Api";
        var audience = _config["Jwt:Audience"] ?? "AlgoRhythm.Client";
        var minutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "15");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("security_stamp", user.SecurityStamp ?? string.Empty)
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

        // Generate Refresh Token
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, ipAddress);

        var userDto = MapToUserDto(user);

        return new AuthResponse(
            tokenString, 
            expires, 
            userDto, 
            refreshToken.Token, 
            refreshToken.ExpiresAt
        );
    }

    private async Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, string ipAddress)
    {
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = GenerateSecureRandomToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7), // Long-lived refresh token
            CreatedByIp = ipAddress
        };

        _context.Set<RefreshToken>().Add(refreshToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Refresh token generated for user: {UserId}", userId);

        return refreshToken;
    }

    private static string GenerateSecureRandomToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
            throw new UserNotFoundException();
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Login attempt with invalid password for user: {Email}", user.Email);
            throw new InvalidPasswordException();
        }

        if (!user.EmailConfirmed)
        {
            _logger.LogWarning("Login attempt with unverified email: {Email}", user.Email);
            throw new EmailNotVerifiedException();
        }

        // Update login streak
        try
        {
            await _streakService.UpdateLoginStreakAsync(user.Id);
            _logger.LogInformation("Login streak updated for user: {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update login streak for user {UserId}", user.Id);
            // Don't throw - login should still succeed
        }

        _logger.LogInformation("User logged in successfully: {Email}", user.Email);

        return await GenerateAuthResponseAsync(user, "unknown");
    }

    public async Task<RefreshTokenResponseDto> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        var token = await _context.Set<RefreshToken>()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token == null || !token.IsActive)
        {
            _logger.LogWarning("Refresh token is invalid or expired");
            throw new InvalidRefreshTokenException();
        }

        // Generate new refresh token and revoke old one
        var newRefreshToken = await RotateRefreshTokenAsync(token, ipAddress);
        
        // Generate new access token
        var user = token.User;
        var roles = await _userManager.GetRolesAsync(user);

        var key = _config["Jwt:Key"] 
            ?? Environment.GetEnvironmentVariable("JWT_KEY") 
            ?? throw new InvalidOperationException("JWT key is not configured.");
        
        var issuer = _config["Jwt:Issuer"] ?? "AlgoRhythm.Api";
        var audience = _config["Jwt:Audience"] ?? "AlgoRhythm.Client";
        var minutes = int.Parse(_config["Jwt:ExpiresMinutes"] ?? "15");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("security_stamp", user.SecurityStamp ?? string.Empty)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(minutes);
        var jwtToken = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(jwtToken);

        _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.Id);

        return new RefreshTokenResponseDto(
            tokenString,
            expires,
            newRefreshToken.Token,
            newRefreshToken.ExpiresAt
        );
    }

    private async Task<RefreshToken> RotateRefreshTokenAsync(RefreshToken token, string ipAddress)
    {
        var newRefreshToken = new RefreshToken
        {
            UserId = token.UserId,
            Token = GenerateSecureRandomToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = ipAddress
        };

        // Revoke old token
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReplacedByToken = newRefreshToken.Token;

        _context.Set<RefreshToken>().Add(newRefreshToken);
        await _context.SaveChangesAsync();

        return newRefreshToken;
    }

    public async Task RevokeTokenAsync(string refreshToken, string ipAddress)
    {
        var token = await _context.Set<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token == null || !token.IsActive)
        {
            _logger.LogWarning("Attempt to revoke invalid or expired refresh token");
            throw new InvalidRefreshTokenException();
        }

        // Revoke token
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Refresh token revoked for user: {UserId}", token.UserId);
    }

    private async Task RevokeAllUserTokensAsync(Guid userId, string reason)
    {
        var tokens = await _context.Set<RefreshToken>()
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = reason;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("All refresh tokens revoked for user: {UserId}. Reason: {Reason}", userId, reason);
    }

    public async Task<UserDto> UpdateUserProfileAsync(Guid userId, UpdateUserProfileDto request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("Profile update attempt for non-existent user: {UserId}", userId);
            throw new UserNotFoundException();
        }

        bool emailChanged = false;
        string? oldEmail = user.Email;

        // Update FirstName if provided
        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            user.FirstName = request.FirstName.Trim();
        }

        // Update LastName if provided
        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            user.LastName = request.LastName.Trim();
        }

        // Update Email if provided and different
        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
        {
            // Validate email format
            if (!request.Email.Contains("@"))
            {
                throw new ArgumentException("Invalid email format.");
            }

            // Check if new email is already taken
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null && existingUser.Id != userId)
            {
                _logger.LogWarning("Profile update attempt with existing email: {Email}", request.Email);
                throw new EmailAlreadyExistsException();
            }

            emailChanged = true;
            user.Email = request.Email.Trim();
            user.UserName = request.Email.Trim(); // Update username to match email
            user.NormalizedEmail = request.Email.Trim().ToUpperInvariant();
            user.NormalizedUserName = request.Email.Trim().ToUpperInvariant();
        }

        // Update timestamp
        user.UpdatedAt = DateTime.UtcNow;

        // Save changes
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update user profile: {errors}");
        }

        _logger.LogInformation("User profile updated successfully: {UserId}", userId);

        // Send email notification if email was changed
        if (emailChanged && !string.IsNullOrEmpty(user.Email))
        {
            await SendEmailChangeNotificationAsync(user, oldEmail!);
        }

        // Return updated user data
        return MapToUserDto(user);
    }

    private async Task SendEmailChangeNotificationAsync(User user, string oldEmail)
    {
        var subject = "Email Address Changed - AlgoRhythm";

        var plain = $@"Hello {user.FirstName}!

Your email address for your AlgoRhythm account has been successfully changed.

Old email: {oldEmail}
New email: {user.Email}

If you did not make this change, please contact our support team immediately.

Best regards,
AlgoRhythm Team

---
This is an automated message, please do not reply.";

        var html = $@"<p>Hello <strong>{user.FirstName}</strong>!</p>

<p>Your email address for your AlgoRhythm account has been successfully changed.</p>

<table style='margin: 20px 0;'>
<tr>
    <td style='padding: 5px; font-weight: bold;'>Old email:</td>
    <td style='padding: 5px;'>{oldEmail}</td>
</tr>
<tr>
    <td style='padding: 5px; font-weight: bold;'>New email:</td>
    <td style='padding: 5px;'>{user.Email}</td>
</tr>
</table>

<p><strong>If you did not make this change, please contact our support team immediately.</strong></p>

<p>Best regards,<br>AlgoRhythm Team</p>

<hr>
<p style='font-size: 12px; color: #666;'>This is an automated message, please do not reply.</p>";

        try
        {
            await _emailSender.SendEmailAsync(user.Email!, subject, plain, html);
            _logger.LogInformation("Email change notification sent to: {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email change notification to: {Email}", user.Email);
            // Don't throw - profile was already updated successfully
        }
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            EmailConfirmed = user.EmailConfirmed,
            CreatedAt = user.CreatedAt,
            CurrentStreak = user.CurrentStreak,
            LongestStreak = user.LongestStreak,
            LastLoginDate = user.LastLoginDate
        };
    }
}