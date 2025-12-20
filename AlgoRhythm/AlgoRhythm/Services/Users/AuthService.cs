using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Services.Users.Exceptions;
using AlgoRhythm.Services.Courses.Interfaces;
using AlgoRhythm.Shared.Dtos.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using User = AlgoRhythm.Shared.Models.Users.User;

namespace AlgoRhythm.Services.Users;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;
    private readonly ICourseProgressService _courseProgressService;
    
    // Simple in-memory rate limiting
    private static readonly Dictionary<string, DateTime> _lastEmailSent = new();
    private static readonly TimeSpan _emailCooldown = TimeSpan.FromMinutes(1);

    public AuthService(
        UserManager<User> userManager,
        IEmailSender emailSender,
        IConfiguration config,
        ILogger<AuthService> logger,
        ICourseProgressService courseProgressService)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _config = config;
        _logger = logger;
        _courseProgressService = courseProgressService;
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

        // Reset security stamp
        user.SecurityStamp = Guid.NewGuid().ToString();
        await _userManager.UpdateAsync(user);

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
            return await GenerateAuthResponseAsync(user);
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

        return await GenerateAuthResponseAsync(user);
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        // Create JWT - USE ENVIRONMENT VARIABLE
        var key = _config["Jwt:Key"] 
            ?? Environment.GetEnvironmentVariable("JWT_KEY") 
            ?? throw new InvalidOperationException("JWT key is not configured.");
        
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

        _logger.LogInformation("User logged in successfully: {Email}", user.Email);

        return await GenerateAuthResponseAsync(user);
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
        return new UserDto(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.CreatedAt
        );
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
}