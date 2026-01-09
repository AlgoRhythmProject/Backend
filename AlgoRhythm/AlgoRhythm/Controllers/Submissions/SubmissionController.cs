using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AlgoRhythm.Shared.Dtos.Submissions;
using AlgoRhythm.Services.Submissions.Interfaces;

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
}
