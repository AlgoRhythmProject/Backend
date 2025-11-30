using AlgoRhythm.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AlgoRhythm.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CourseProgressController : ControllerBase
{
    private readonly ICourseProgressService _service;
    private readonly ILogger<CourseProgressController> _logger;

    public CourseProgressController(ICourseProgressService service, ILogger<CourseProgressController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("my-progress")]
    public async Task<IActionResult> GetMyProgress(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid user ID in token" });

        var progresses = await _service.GetByUserIdAsync(userId, ct);
        return Ok(progresses);
    }

    [HttpGet("my-progress/{courseId:guid}")]
    public async Task<IActionResult> GetMyCourseProgress(Guid courseId, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid user ID in token" });

        var progress = await _service.GetByUserAndCourseAsync(userId, courseId, ct);
        if (progress == null)
            return NotFound(new { error = "Progress not found" });

        return Ok(progress);
    }

    [HttpPost("start/{courseId:guid}")]
    public async Task<IActionResult> StartCourse(Guid courseId, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid user ID in token" });

        try
        {
            var progress = await _service.StartCourseAsync(userId, courseId, ct);
            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting course");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("update/{courseId:guid}")]
    public async Task<IActionResult> UpdateProgress(Guid courseId, [FromBody] int percentage, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid user ID in token" });

        try
        {
            await _service.UpdateProgressAsync(userId, courseId, percentage, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Progress not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating progress");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("complete/{courseId:guid}")]
    public async Task<IActionResult> CompleteCourse(Guid courseId, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid user ID in token" });

        try
        {
            await _service.CompleteCourseAsync(userId, courseId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Progress not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing course");
            return BadRequest(new { error = ex.Message });
        }
    }
}