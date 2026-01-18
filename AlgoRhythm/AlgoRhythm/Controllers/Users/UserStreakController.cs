using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Dtos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AlgoRhythm.Controllers.Users;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserStreakController : ControllerBase
{
    private readonly IUserStreakService _streakService;
    private readonly ILogger<UserStreakController> _logger;

    public UserStreakController(
        IUserStreakService streakService,
        ILogger<UserStreakController> logger)
    {
        _streakService = streakService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's login streak information
    /// </summary>
    [HttpGet("my-streak")]
    [ProducesResponseType(typeof(UserStreakDto), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetMyStreak(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid user token" });

        try
        {
            var streak = await _streakService.GetUserStreakAsync(userId, ct);
            return Ok(streak);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get specific user's login streak information (Admin only)
    /// </summary>
    [HttpGet("{userId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserStreakDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUserStreak(Guid userId, CancellationToken ct)
    {
        try
        {
            var streak = await _streakService.GetUserStreakAsync(userId, ct);
            return Ok(streak);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Manually update login streak (for testing purposes - Admin only)
    /// </summary>
    [HttpPost("{userId:guid}/update")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> UpdateStreak(Guid userId, CancellationToken ct)
    {
        try
        {
            await _streakService.UpdateLoginStreakAsync(userId, ct);
            return Ok(new { message = "Streak updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update streak for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }
}