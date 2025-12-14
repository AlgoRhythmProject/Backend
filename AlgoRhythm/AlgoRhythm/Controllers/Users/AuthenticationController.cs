using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlgoRhythm.Shared.Dtos.Users;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Services.Users.Exceptions;

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
                Secure = true, // HTTPS only
                SameSite = SameSiteMode.Strict,
                Expires = authResponse.ExpiresUtc
            });

            // Return full response with token and user data
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

            // Set JWT in HTTP-only cookie
            Response.Cookies.Append("JWT", authResponse.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // HTTPS only
                SameSite = SameSiteMode.Strict,
                Expires = authResponse.ExpiresUtc
            });

            // Return full response with token and user data
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
    /// User logout (stateless JWT — client must delete the token).
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult Logout()
    {
        // Delete JWT cookie
        Response.Cookies.Delete("JWT");

        _logger.LogInformation("User logged out (JWT cookie deleted)");
        return Ok(new { message = "Logged out successfully." });
    }
}

/// <summary>
/// Standardized error response for API
/// </summary>
public record ErrorResponse(string Code, string Message);