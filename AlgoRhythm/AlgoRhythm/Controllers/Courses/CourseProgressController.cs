using AlgoRhythm.Services.Courses.Interfaces;
using AlgoRhythm.Shared.Dtos.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AlgoRhythm.Controllers.Courses;

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

    /// <summary>
    /// Get all course progress for current user
    /// </summary>
    [HttpGet("my-progress")]
    [ProducesResponseType(typeof(IEnumerable<CourseProgressDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetMyProgress(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid user ID in token" });

        var progresses = await _service.GetByUserIdAsync(userId, ct);
        return Ok(progresses);
    }

    /// <summary>
    /// Get progress for specific course
    /// </summary>
    [HttpGet("my-progress/{courseId:guid}")]
    [ProducesResponseType(typeof(CourseProgressDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
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

    /// <summary>
    /// Toggle lecture completion status (complete/uncomplete)
    /// </summary>
    [HttpPost("lecture/{lectureId:guid}/toggle")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ToggleLectureCompletion(Guid lectureId, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid user ID in token" });

        try
        {
            var isCompleted = await _service.ToggleLectureCompletionAsync(userId, lectureId, ct);
            return Ok(new 
            { 
                message = isCompleted ? "Lecture marked as completed" : "Lecture marked as incomplete",
                isCompleted,
                lectureId
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling lecture completion");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Mark lecture as completed
    /// </summary>
    [HttpPost("lecture/{lectureId:guid}/complete")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> MarkLectureAsCompleted(Guid lectureId, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid user ID in token" });

        try
        {
            var result = await _service.MarkLectureAsCompletedAsync(userId, lectureId, ct);
            
            if (!result)
                return Ok(new { message = "Lecture already completed", lectureId });

            return Ok(new { message = "Lecture marked as completed", lectureId });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking lecture as completed");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Mark lecture as incomplete
    /// </summary>
    [HttpPost("lecture/{lectureId:guid}/uncomplete")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> MarkLectureAsIncomplete(Guid lectureId, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid user ID in token" });

        try
        {
            var result = await _service.MarkLectureAsIncompletedAsync(userId, lectureId, ct);
            
            if (!result)
                return Ok(new { message = "Lecture already incomplete", lectureId });

            return Ok(new { message = "Lecture marked as incomplete", lectureId });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking lecture as incomplete");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Manually recalculate course progress (useful after submission)
    /// </summary>
    [HttpPost("recalculate/{courseId:guid}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> RecalculateProgress(Guid courseId, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid user ID in token" });

        try
        {
            await _service.RecalculateProgressAsync(userId, courseId, ct);
            return Ok(new { message = "Progress recalculated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating progress");
            return BadRequest(new { error = ex.Message });
        }
    }
}