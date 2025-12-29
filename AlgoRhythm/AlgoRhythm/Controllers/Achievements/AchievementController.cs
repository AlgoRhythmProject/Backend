using AlgoRhythm.Services.Achievements.Interfaces;
using AlgoRhythm.Shared.Dtos.Achievements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AlgoRhythm.Controllers.Achievements;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AchievementController : ControllerBase
{
    private readonly IAchievementService _service;
    private readonly ILogger<AchievementController> _logger;

    public AchievementController(
        IAchievementService service,
        ILogger<AchievementController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all available achievements
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AchievementDto>), 200)]
    public async Task<IActionResult> GetAllAchievements(CancellationToken ct)
    {
        var achievements = await _service.GetAllAchievementsAsync(ct);
        return Ok(achievements);
    }

    /// <summary>
    /// Get specific achievement details
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AchievementDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAchievementById(Guid id, CancellationToken ct)
    {
        var achievement = await _service.GetAchievementByIdAsync(id, ct);
        if (achievement == null)
            return NotFound(new { error = "Achievement not found" });

        return Ok(achievement);
    }

    /// <summary>
    /// Get all achievements for current user with progress
    /// </summary>
    [HttpGet("my-achievements")]
    [ProducesResponseType(typeof(IEnumerable<UserAchievementDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetMyAchievements(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid user ID in token" });

        var achievements = await _service.GetUserAchievementsAsync(userId, ct);
        return Ok(achievements);
    }

    /// <summary>
    /// Get specific achievement progress for current user
    /// </summary>
    [HttpGet("my-achievements/{achievementId:guid}")]
    [ProducesResponseType(typeof(UserAchievementDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetMyAchievement(Guid achievementId, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid user ID in token" });

        var achievement = await _service.GetUserAchievementAsync(userId, achievementId, ct);
        if (achievement == null)
            return NotFound(new { error = "Achievement not found" });

        return Ok(achievement);
    }

    /// <summary>
    /// Manually refresh achievement progress (useful for testing)
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> RefreshAchievements(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid user ID in token" });

        try
        {
            await _service.CheckAndUpdateAchievementsAsync(userId, ct);
            return Ok(new { message = "Achievements refreshed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing achievements for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }
}