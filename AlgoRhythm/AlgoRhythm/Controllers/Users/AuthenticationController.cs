using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlgoRhythm.Shared.Dtos.Users;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Services.Users.Exceptions;
using System.Security.Claims;

namespace AlgoRhythm.Controllers.Users;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(IAuthService auth, ILogger<AuthenticationController> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user and sends a verification code to the provided email address.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        try
        {
            await _auth.RegisterAsync(req);
            return Ok(new { message = "Registration successful. Check your email for verification code." });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Registration validation error");
            return BadRequest(new ErrorResponse("VALIDATION_ERROR", ex.Message));
        }
        catch (EmailAlreadyExistsException ex)
        {
            _logger.LogWarning(ex, "Email already exists");
            return BadRequest(new ErrorResponse("EMAIL_EXISTS", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed");
            return BadRequest(new ErrorResponse("REGISTRATION_FAILED", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration");
            return StatusCode(500, new ErrorResponse("INTERNAL_ERROR", "An error occurred during registration. Please try again later."));
        }
    }

    /// <summary>
    /// Resends verification code to user's email.
    /// </summary>
    [HttpPost("resend-verification-code")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 429)]
    public async Task<IActionResult> ResendVerificationCode([FromBody] ResendVerificationCodeDto req)
    {
        try
        {
            await _auth.ResendVerificationCodeAsync(req);
            return Ok(new { message = "Verification code has been resent. Check your email." });
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found for resend verification");
            return BadRequest(new ErrorResponse("USER_NOT_FOUND", ex.Message));
        }
        catch (EmailAlreadyVerifiedException ex)
        {
            _logger.LogWarning(ex, "Email already verified");
            return BadRequest(new ErrorResponse("EMAIL_ALREADY_VERIFIED", ex.Message));
        }
        catch (TooManyRequestsException ex)
        {
            _logger.LogWarning(ex, "Too many resend attempts");
            return StatusCode(429, new ErrorResponse("TOO_MANY_REQUESTS", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during resend verification code");
            return StatusCode(500, new ErrorResponse("INTERNAL_ERROR", "An error occurred. Please try again later."));
        }
    }

    /// <summary>
    /// Email verification using a code sent to the user's email.
    /// After successful verification, user is automatically logged in.
    /// </summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest req)
    {
        try
        {
            var authResponse = await _auth.VerifyEmailAsync(req);
            
            // Set JWT in HTTP-only cookie (automatic login)
            Response.Cookies.Append("JWT", authResponse.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = authResponse.ExpiresUtc
            });

            return Ok(authResponse);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found during email verification");
            return BadRequest(new ErrorResponse("USER_NOT_FOUND", ex.Message));
        }
        catch (InvalidVerificationCodeException ex)
        {
            _logger.LogWarning(ex, "Invalid verification code");
            return BadRequest(new ErrorResponse("INVALID_CODE", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during email verification");
            return StatusCode(500, new ErrorResponse("INTERNAL_ERROR", "An error occurred. Please try again later."));
        }
    }

    /// <summary>
    /// User login. Returns JWT token and user data.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        try
        {
            var authResponse = await _auth.LoginAsync(req);

            Response.Cookies.Append("JWT", authResponse.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = authResponse.ExpiresUtc
            });

            return Ok(authResponse);
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found during login");
            return Unauthorized(new ErrorResponse("USER_NOT_FOUND", ex.Message));
        }
        catch (InvalidPasswordException ex)
        {
            _logger.LogWarning(ex, "Invalid password during login for email: {Email}", req.Email);
            return Unauthorized(new ErrorResponse("INVALID_PASSWORD", ex.Message));
        }
        catch (EmailNotVerifiedException ex)
        {
            _logger.LogWarning(ex, "Email not verified during login for email: {Email}", req.Email);
            return BadRequest(new ErrorResponse("EMAIL_NOT_VERIFIED", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login");
            return StatusCode(500, new ErrorResponse("INTERNAL_ERROR", "An error occurred. Please try again later."));
        }
    }

    /// <summary>
    /// Initiates password reset process by sending reset code to user's email.
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 429)]
    public async Task<IActionResult> ForgotPassword([FromBody] ResetPasswordRequestDto req)
    {
        try
        {
            await _auth.RequestPasswordResetAsync(req);
            return Ok(new { message = "If an account exists with this email, a password reset code has been sent." });
        }
        catch (EmailNotVerifiedException ex)
        {
            _logger.LogWarning(ex, "Forgot password attempt for unverified email");
            return BadRequest(new ErrorResponse("EMAIL_NOT_VERIFIED", ex.Message));
        }
        catch (TooManyRequestsException ex)
        {
            _logger.LogWarning(ex, "Too many forgot password attempts");
            return StatusCode(429, new ErrorResponse("TOO_MANY_REQUESTS", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during forgot password");
            return StatusCode(500, new ErrorResponse("INTERNAL_ERROR", "An error occurred. Please try again later."));
        }
    }

    /// <summary>
    /// Resets user password using verification code sent via email.
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto req)
    {
        try
        {
            await _auth.ResetPasswordAsync(req);
            return Ok(new { message = "Password has been reset successfully. You can now login with your new password." });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Password reset validation error");
            return BadRequest(new ErrorResponse("VALIDATION_ERROR", ex.Message));
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found during password reset");
            return BadRequest(new ErrorResponse("USER_NOT_FOUND", ex.Message));
        }
        catch (InvalidVerificationCodeException ex)
        {
            _logger.LogWarning(ex, "Invalid reset code");
            return BadRequest(new ErrorResponse("INVALID_CODE", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Password reset failed");
            return BadRequest(new ErrorResponse("RESET_FAILED", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during password reset");
            return StatusCode(500, new ErrorResponse("INTERNAL_ERROR", "An error occurred. Please try again later."));
        }
    }

    /// <summary>
    /// Changes password for authenticated user.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto req)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new ErrorResponse("UNAUTHORIZED", "User not authenticated."));
            }

            await _auth.ChangePasswordAsync(userId, req);
            return Ok(new { message = "Password has been changed successfully." });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Password change validation error");
            return BadRequest(new ErrorResponse("VALIDATION_ERROR", ex.Message));
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found during password change");
            return BadRequest(new ErrorResponse("USER_NOT_FOUND", ex.Message));
        }
        catch (InvalidPasswordException ex)
        {
            _logger.LogWarning(ex, "Invalid current password during password change");
            return BadRequest(new ErrorResponse("INVALID_CURRENT_PASSWORD", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Password change failed");
            return BadRequest(new ErrorResponse("CHANGE_FAILED", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during password change");
            return StatusCode(500, new ErrorResponse("INTERNAL_ERROR", "An error occurred. Please try again later."));
        }
    }

    /// <summary>
    /// User logout (stateless JWT — client must delete the token).
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("JWT");
        _logger.LogInformation("User logged out (JWT cookie deleted)");
        return Ok(new { message = "Logged out successfully." });
    }
}

/// <summary>
/// Standardized error response for API
/// </summary>
public record ErrorResponse(string Code, string Message);