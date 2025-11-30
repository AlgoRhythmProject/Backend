using AlgoRhythm.Services.Courses.Interfaces;
using AlgoRhythm.Shared.Dtos.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlgoRhythm.Controllers.Courses;

[ApiController]
[Route("api/[controller]")]
public class LectureController : ControllerBase
{
    private readonly ILectureService _service;
    private readonly ILogger<LectureController> _logger;

    public LectureController(ILectureService service, ILogger<LectureController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var lectures = await _service.GetAllAsync(ct);
        return Ok(lectures);
    }

    [HttpGet("course/{courseId:guid}")]
    public async Task<IActionResult> GetByCourse(Guid courseId, CancellationToken ct)
    {
        var lectures = await _service.GetByCourseIdAsync(courseId, ct);
        return Ok(lectures);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var lecture = await _service.GetByIdAsync(id, ct);
        if (lecture == null)
            return NotFound(new { error = "Lecture not found" });

        return Ok(lecture);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] LectureInputDto dto, CancellationToken ct)
    {
        try
        {
            var created = await _service.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lecture");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] LectureInputDto dto, CancellationToken ct)
    {
        try
        {
            await _service.UpdateAsync(id, dto, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Lecture not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lecture");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{lectureId:guid}/tags/{tagId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddTag(Guid lectureId, Guid tagId, CancellationToken ct)
    {
        try
        {
            await _service.AddTagAsync(lectureId, tagId, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tag to lecture");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{lectureId:guid}/tags/{tagId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveTag(Guid lectureId, Guid tagId, CancellationToken ct)
    {
        try
        {
            await _service.RemoveTagAsync(lectureId, tagId, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tag from lecture");
            return BadRequest(new { error = ex.Message });
        }
    }
}