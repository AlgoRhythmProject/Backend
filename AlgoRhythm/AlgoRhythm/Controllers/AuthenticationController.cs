using AlgoRhythm.Dtos;
using AlgoRhythm.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlgoRhythm.Controllers;

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
    /// Rejestracja nowego użytkownika. Wysyła kod weryfikacyjny na email.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
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
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration");
            return StatusCode(500, new { error = "An error occurred during registration. Please try again later." });
        }
    }

    /// <summary>
    /// Weryfikacja adresu email za pomocą kodu wysłanego na email.
    /// Po pomyślnej weryfikacji użytkownik jest automatycznie zalogowany.
    /// </summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest req)
    {
        try
        {
            var authResponse = await _auth.VerifyEmailAsync(req);
            
            // Ustaw JWT w HTTP-only cookie (automatyczne logowanie)
            Response.Cookies.Append("JWT", authResponse.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Tylko HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = authResponse.ExpiresUtc
            });

            // Zwróć pełną odpowiedź z tokenem i danymi użytkownika
            return Ok(authResponse);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Email verification failed");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during email verification");
            return StatusCode(500, new { error = "An error occurred. Please try again later." });
        }
    }

    /// <summary>
    /// Logowanie użytkownika. Zwraca JWT token oraz dane użytkownika.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        try
        {
            var authResponse = await _auth.LoginAsync(req);

            // Ustaw JWT w HTTP-only cookie
            Response.Cookies.Append("JWT", authResponse.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Tylko HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = authResponse.ExpiresUtc
            });

            // Zwróć pełną odpowiedź z tokenem i danymi użytkownika
            return Ok(authResponse);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Login failed for email: {Email}", req.Email);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login");
            return StatusCode(500, new { error = "An error occurred. Please try again later." });
        }
    }

    //TODO: można zrobić Blacklist tokenów po stronie serwera. Podobno jest lepsze ale jest trudniejsze

    /// <summary>
    /// Wylogowanie (stateless JWT — klient musi usunąć token).
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(200)]
    public IActionResult Logout()
    {
        // Usuń cookie JWT
        Response.Cookies.Delete("JWT");

        _logger.LogInformation("User logged out (JWT cookie deleted)");
        return Ok(new { message = "Logged out successfully." });
    }
}