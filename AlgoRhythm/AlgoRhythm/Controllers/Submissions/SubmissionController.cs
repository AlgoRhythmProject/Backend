using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AlgoRhythm.Shared.Dtos.Submissions;
using AlgoRhythm.Services.Submissions.Interfaces;

namespace AlgoRhythm.Controllers.Submissions;


[ApiController]
[Route("api/[controller]")]
public class SubmissionsController(ISubmissionService submissions, ILogger<SubmissionsController> logger) : ControllerBase
{
    private readonly ISubmissionService _submissions = submissions;
    private readonly ILogger<SubmissionsController> _logger = logger;

    [HttpPost("programming")]
    [Authorize]
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

    [HttpGet("{submissionId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetSubmission(Guid submissionId, CancellationToken ct)
    {
        var dto = await _submissions.GetSubmissionAsync(submissionId, ct);
        if (dto == null) return NotFound();
        return Ok(dto);
    }
}
