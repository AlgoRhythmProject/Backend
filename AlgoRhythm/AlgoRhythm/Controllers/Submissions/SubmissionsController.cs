using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AlgoRhythm.Shared.Dtos.Submissions;
using AlgoRhythm.Services.Submissions.Interfaces;
using AlgoRhythm.Shared.Models.Users;

namespace AlgoRhythm.Controllers.Submissions;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SubmissionsController(ISubmissionService submissions, ILogger<SubmissionsController> logger) : ControllerBase
{
    private readonly ISubmissionService _submissions = submissions;
    private readonly ILogger<SubmissionsController> _logger = logger;

    /// <summary>
    /// Submits a programming task solution for evaluation.
    /// </summary>
    /// <param name="req">Submission request data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Submission result with test results</returns>
    [HttpPost("programming")]
    public async Task<IActionResult> SubmitProgramming([FromBody] SubmitProgrammingRequestDto req, CancellationToken ct)
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { error = "Invalid user id in token." });


            var response = await _submissions.CreateProgrammingSubmissionAsync(userId, req, ct);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Submission validation error");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in SubmitProgramming");
            return StatusCode(500, new { error = "Internal server error." });
        }
    }

    /// <summary>
    /// Gets a submission by its ID.
    /// </summary>
    /// <param name="submissionId">The submission ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Submission details</returns>
    [HttpGet("{submissionId:guid}")]
    public async Task<IActionResult> GetSubmission(Guid submissionId, CancellationToken ct)
    {
        var dto = await _submissions.GetSubmissionAsync(submissionId, ct);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    /// <summary>
    /// Get all submissions (Admin only)
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IEnumerable<SubmissionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<SubmissionResponseDto>>> GetAllSubmissions(CancellationToken ct)
    {
        try
        {
            var submissions = await _submissions.GetAllSubmissionsAsync(ct);
            return Ok(submissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all submissions");
            return StatusCode(500, new { error = "An error occurred while retrieving submissions" });
        }
    }

    /// <summary>
    /// Get current user's submissions
    /// </summary>
    [HttpGet("my-submissions")]
    [ProducesResponseType(typeof(IEnumerable<SubmissionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<SubmissionResponseDto>>> GetMySubmissions(CancellationToken ct)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "Invalid user token" });
            }

            var submissions = await _submissions.GetSubmissionsByUserIdAsync(userId, ct);
            return Ok(submissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user's submissions");
            return StatusCode(500, new { error = "An error occurred while retrieving submissions" });
        }
    }

    /// <summary>
    /// Get current user's submissions for a specific task
    /// </summary>
    [HttpGet("my-submissions/task/{taskId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<SubmissionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<SubmissionResponseDto>>> GetMySubmissionsForTask(Guid taskId, CancellationToken ct)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "Invalid user token" });
            }

            var submissions = await _submissions.GetSubmissionsByUserAndTaskAsync(userId, taskId, ct);
            return Ok(submissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user's submissions for task {TaskId}", taskId);
            return StatusCode(500, new { error = "An error occurred while retrieving submissions" });
        }
    }

    /// <summary>
    /// Get recent submissions with pagination (Admin only)
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    [HttpGet("recent")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IEnumerable<SubmissionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<SubmissionResponseDto>>> GetRecentSubmissions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            if (page < 1)
            {
                return BadRequest(new { error = "Page number must be greater than 0" });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { error = "Page size must be between 1 and 100" });
            }

            var submissions = await _submissions.GetRecentSubmissionsAsync(page, pageSize, ct);
            
            // Add pagination metadata to response headers
            Response.Headers.Append("X-Page", page.ToString());
            Response.Headers.Append("X-Page-Size", pageSize.ToString());
            
            return Ok(submissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent submissions (page: {Page}, pageSize: {PageSize})", page, pageSize);
            return StatusCode(500, new { error = "An error occurred while retrieving submissions" });
        }
    }
}
