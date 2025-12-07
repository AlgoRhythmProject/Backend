using AlgoRhythm.Services.Courses.Interfaces;
using AlgoRhythm.Shared.Dtos.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlgoRhythm.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CourseController : ControllerBase
{
    private readonly ICourseService _service;

    public CourseController(ICourseService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseSummaryDto>>> GetAll(CancellationToken ct)
    {
        var courses = await _service.GetAllAsync(ct);
        return Ok(courses);
    }

    [HttpGet("published")]
    public async Task<ActionResult<IEnumerable<CourseSummaryDto>>> GetPublished(CancellationToken ct)
    {
        var courses = await _service.GetPublishedAsync(ct);
        return Ok(courses);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CourseDto>> GetById(Guid id, CancellationToken ct)
    {
        var course = await _service.GetByIdAsync(id, ct);
        if (course == null)
            return NotFound();

        return Ok(course);
    }

    [HttpPost]
    public async Task<ActionResult<CourseDto>> Create([FromBody] CourseInputDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CourseInputDto dto, CancellationToken ct)
    {
        try
        {
            await _service.UpdateAsync(id, dto, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{courseId:guid}/tasks/{taskId:guid}")]
    public async Task<IActionResult> AddTask(Guid courseId, Guid taskId, CancellationToken ct)
    {
        await _service.AddTaskToCourseAsync(courseId, taskId, ct);
        return NoContent();
    }

    [HttpDelete("{courseId:guid}/tasks/{taskId:guid}")]
    public async Task<IActionResult> RemoveTask(Guid courseId, Guid taskId, CancellationToken ct)
    {
        await _service.RemoveTaskFromCourseAsync(courseId, taskId, ct);
        return NoContent();
    }

    [HttpPost("{courseId:guid}/lectures/{lectureId:guid}")]
    public async Task<IActionResult> AddLecture(Guid courseId, Guid lectureId, CancellationToken ct)
    {
        await _service.AddLectureToCourseAsync(courseId, lectureId, ct);
        return NoContent();
    }

    [HttpDelete("{courseId:guid}/lectures/{lectureId:guid}")]
    public async Task<IActionResult> RemoveLecture(Guid courseId, Guid lectureId, CancellationToken ct)
    {
        await _service.RemoveLectureFromCourseAsync(courseId, lectureId, ct);
        return NoContent();
    }
}