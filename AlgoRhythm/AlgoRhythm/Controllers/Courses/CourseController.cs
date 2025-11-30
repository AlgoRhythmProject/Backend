using AlgoRhythm.Services.Courses.Interfaces;
using AlgoRhythm.Shared.Dtos.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlgoRhythm.Controllers.Courses;

[ApiController]
[Route("api/[controller]")]
public class CourseController : ControllerBase
{
    private readonly ICourseService _service;
    private readonly ILogger<CourseController> _logger;

    public CourseController(ICourseService service, ILogger<CourseController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var courses = await _service.GetAllAsync(ct);
        return Ok(courses);
    }

    [HttpGet("published")]
    public async Task<IActionResult> GetPublished(CancellationToken ct)
    {
        var courses = await _service.GetPublishedAsync(ct);
        return Ok(courses);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var course = await _service.GetByIdAsync(id, ct);
        if (course == null)
            return NotFound(new { error = "Course not found" });

        return Ok(course);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CourseInputDto dto, CancellationToken ct)
    {
        try
        {
            var created = await _service.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating course");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CourseInputDto dto, CancellationToken ct)
    {
        try
        {
            await _service.UpdateAsync(id, dto, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Course not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course");
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

    // ===== TASK MANAGEMENT =====

    [HttpPost("{courseId:guid}/tasks/{taskId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddTask(Guid courseId, Guid taskId, CancellationToken ct)
    {
        try
        {
            await _service.AddTaskToCourseAsync(courseId, taskId, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding task to course");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{courseId:guid}/tasks/{taskId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveTask(Guid courseId, Guid taskId, CancellationToken ct)
    {
        try
        {
            await _service.RemoveTaskFromCourseAsync(courseId, taskId, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing task from course");
            return BadRequest(new { error = ex.Message });
        }
    }

    // ===== LECTURE MANAGEMENT =====

    [HttpPost("{courseId:guid}/lectures/{lectureId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddLecture(Guid courseId, Guid lectureId, CancellationToken ct)
    {
        try
        {
            await _service.AddLectureToCourseAsync(courseId, lectureId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding lecture to course");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{courseId:guid}/lectures/{lectureId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveLecture(Guid courseId, Guid lectureId, CancellationToken ct)
    {
        try
        {
            await _service.RemoveLectureFromCourseAsync(courseId, lectureId, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing lecture from course");
            return BadRequest(new { error = ex.Message });
        }
    }

    // ===== TAG MANAGEMENT =====

    [HttpPost("{courseId:guid}/tags/{tagId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddTag(Guid courseId, Guid tagId, CancellationToken ct)
    {
        try
        {
            await _service.AddTagToCourseAsync(courseId, tagId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tag to course");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{courseId:guid}/tags/{tagId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveTag(Guid courseId, Guid tagId, CancellationToken ct)
    {
        try
        {
            await _service.RemoveTagFromCourseAsync(courseId, tagId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tag from course");
            return BadRequest(new { error = ex.Message });
        }
    }
}