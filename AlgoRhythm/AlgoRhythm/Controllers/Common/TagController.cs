using AlgoRhythm.Services.Common.Interfaces;
using AlgoRhythm.Shared.Dtos.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlgoRhythm.Controllers.Common;

[ApiController]
[Route("api/[controller]")]
public class TagController : ControllerBase
{
    private readonly ITagService _service;
    private readonly ILogger<TagController> _logger;

    public TagController(ITagService service, ILogger<TagController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all tags.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of all tags</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tags = await _service.GetAllAsync(ct);
        return Ok(tags);
    }

    /// <summary>
    /// Gets a tag by its ID.
    /// </summary>
    /// <param name="id">The tag ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tag details</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var tag = await _service.GetByIdAsync(id, ct);
        if (tag == null)
            return NotFound(new { error = "Tag not found" });

        return Ok(tag);
    }

    /// <summary>
    /// Gets a tag by its name.
    /// </summary>
    /// <param name="name">The tag name</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tag details</returns>
    [HttpGet("by-name/{name}")]
    public async Task<IActionResult> GetByName(string name, CancellationToken ct)
    {
        var tag = await _service.GetByNameAsync(name, ct);
        if (tag == null)
            return NotFound(new { error = "Tag not found" });

        return Ok(tag);
    }

    /// <summary>
    /// Creates a new tag. Admin only.
    /// </summary>
    /// <param name="dto">Tag input data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created tag</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] TagInputDto dto, CancellationToken ct)
    {
        try
        {
            var created = await _service.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tag");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing tag. Admin only.
    /// </summary>
    /// <param name="id">The tag ID</param>
    /// <param name="dto">Updated tag data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TagInputDto dto, CancellationToken ct)
    {
        try
        {
            await _service.UpdateAsync(id, dto, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Tag not found" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tag");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a tag. Admin only.
    /// </summary>
    /// <param name="id">The tag ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}