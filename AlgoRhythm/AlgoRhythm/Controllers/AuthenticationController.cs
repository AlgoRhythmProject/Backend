using AlgoRhythm.Dtos;
using AlgoRhythm.Services;
using Microsoft.AspNetCore.Mvc;

namespace AlgoRhythm.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthenticationController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        try
        {
            await _auth.RegisterAsync(req);
            return Ok(new { message = "Registration successful. Check email for verification code." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest req)
    {
        try
        {
            await _auth.VerifyEmailAsync(req);
            return Ok(new { message = "Email verified. You can now log in." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        try
        {
            var token = await _auth.LoginAsync(req);
            return Ok(token);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Stateless JWT: server cannot "destroy" token; client must delete it.
        // If you implement refresh tokens or server-side token revocation, implement that here.
        return Ok(new { message = "Logout: delete token client-side. For server revocation implement refresh-token store." });
    }
}