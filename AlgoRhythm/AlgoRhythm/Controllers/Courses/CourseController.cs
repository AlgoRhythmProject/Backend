using AlgoRhythm.Services.Courses.Interfaces;
using AlgoRhythm.Shared.Dtos.Courses;
using AlgoRhythm.Shared.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlgoRhythm.Controllers.Courses;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CourseController : ControllerBase
{
    private readonly ICourseService _service;

    public CourseController(ICourseService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets all courses.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of all courses</returns>
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IEnumerable<CourseSummaryDto>>> GetAll(CancellationToken ct)
    {
        var courses = await _service.GetAllAsync(ct);
        return Ok(courses);
    }

    /// <summary>
    /// Gets all published courses.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of published courses</returns>
    [HttpGet("published")]
    public async Task<ActionResult<IEnumerable<CourseSummaryDto>>> GetPublished(CancellationToken ct)
    {
        var courses = await _service.GetPublishedAsync(ct);
        return Ok(courses);
    }

    /// <summary>
    /// Gets a course by its ID.
    /// </summary>
    /// <param name="id">The course ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Course details</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CourseDto>> GetById(Guid id, CancellationToken ct)
    {
        var course = await _service.GetByIdAsync(id, ct);
        if (course == null)
            return NotFound();

        return Ok(course);
    }

    /// <summary>
    /// Creates a new course. Admin only.
    /// </summary>
    /// <param name="dto">Course input data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created course</returns>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CourseDto>> Create([FromBody] CourseInputDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates an existing course. Admin only.
    /// </summary>
    /// <param name="id">The course ID</param>
    /// <param name="dto">Updated course data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
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

    /// <summary>
    /// Deletes a course. Admin only.
    /// </summary>
    /// <param name="id">The course ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Adds a task to a course. Admin only.
    /// </summary>
    /// <param name="courseId">The course ID</param>
    /// <param name="taskId">The task ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("{courseId:guid}/tasks/{taskId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> AddTask(Guid courseId, Guid taskId, CancellationToken ct)
    {
        await _service.AddTaskToCourseAsync(courseId, taskId, ct);
        return NoContent();
    }

    /// <summary>
    /// Removes a task from a course. Admin only.
    /// </summary>
    /// <param name="courseId">The course ID</param>
    /// <param name="taskId">The task ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{courseId:guid}/tasks/{taskId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> RemoveTask(Guid courseId, Guid taskId, CancellationToken ct)
    {
        await _service.RemoveTaskFromCourseAsync(courseId, taskId, ct);
        return NoContent();
    }

    /// <summary>
    /// Adds a lecture to a course. Admin only.
    /// </summary>
    /// <param name="courseId">The course ID</param>
    /// <param name="lectureId">The lecture ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("{courseId:guid}/lectures/{lectureId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> AddLecture(Guid courseId, Guid lectureId, CancellationToken ct)
    {
        await _service.AddLectureToCourseAsync(courseId, lectureId, ct);
        return NoContent();
    }

    /// <summary>
    /// Removes a lecture from a course. Admin only.
    /// </summary>
    /// <param name="courseId">The course ID</param>
    /// <param name="lectureId">The lecture ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{courseId:guid}/lectures/{lectureId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> RemoveLecture(Guid courseId, Guid lectureId, CancellationToken ct)
    {
        await _service.RemoveLectureFromCourseAsync(courseId, lectureId, ct);
        return NoContent();
    }
}