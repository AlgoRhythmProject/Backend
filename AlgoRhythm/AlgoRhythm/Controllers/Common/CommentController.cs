using AlgoRhythm.Services.Common.Interfaces;
using AlgoRhythm.Shared.Dtos.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AlgoRhythm.Controllers.Common;

[ApiController]
[Route("api/[controller]")]
public class CommentController : ControllerBase
{
    private readonly ICommentService _service;
    private readonly ILogger<CommentController> _logger;

    public CommentController(ICommentService service, ILogger<CommentController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all comments for a specific task.
    /// </summary>
    /// <param name="taskId">The ID of the task</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of comments for the task</returns>
    [HttpGet("task/{taskId:guid}")]
    public async Task<IActionResult> GetByTask(Guid taskId, CancellationToken ct)
    {
        var comments = await _service.GetByTaskIdAsync(taskId, ct);
        return Ok(comments);
    }

    /// <summary>
    /// Gets a comment by its ID.
    /// </summary>
    /// <param name="id">The comment ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The comment details</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var comment = await _service.GetByIdAsync(id, ct);
        if (comment == null)
            return NotFound(new { error = "Comment not found" });

        return Ok(comment);
    }

    /// <summary>
    /// Creates a new comment for a task.
    /// </summary>
    /// <param name="dto">Comment input data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created comment</returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CommentInputDto dto, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid user ID in token" });

        try
        {
            var created = await _service.CreateAsync(userId, dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comment");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing comment.
    /// </summary>
    /// <param name="id">The comment ID</param>
    /// <param name="content">Updated comment content</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] string content, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid user ID in token" });

        try
        {
            await _service.UpdateAsync(id, userId, content, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Comment not found" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating comment");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a comment.
    /// </summary>
    /// <param name="id">The comment ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { error = "Invalid user ID in token" });

        try
        {
            await _service.DeleteAsync(id, userId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Comment not found" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment");
            return BadRequest(new { error = ex.Message });
        }
    }
}