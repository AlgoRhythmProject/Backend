using AlgoRhythm.Services.Tasks.Interfaces;
using AlgoRhythm.Shared.Dtos.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlgoRhythm.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TaskController : ControllerBase
{
    private readonly ITaskService _service;

    public TaskController(ITaskService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets all tasks.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of all tasks</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskDto>>> GetAll(CancellationToken ct)
    {
        var tasks = await _service.GetAllAsync(ct);
        return Ok(tasks);
    }

    /// <summary>
    /// Gets all tasks with their associated courses.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of tasks with course information</returns>
    [HttpGet("with-courses")]
    public async Task<ActionResult<IEnumerable<TaskWithCoursesDto>>> GetAllWithCourses(CancellationToken ct)
    {
        var tasks = await _service.GetAllWithCoursesAsync(ct);
        return Ok(tasks);
    }

    /// <summary>
    /// Gets all published tasks.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of published tasks</returns>
    [HttpGet("published")]
    public async Task<ActionResult<IEnumerable<TaskDto>>> GetPublished(CancellationToken ct)
    {
        var tasks = await _service.GetPublishedAsync(ct);
        return Ok(tasks);
    }

    /// <summary>
    /// Gets a task by its ID.
    /// </summary>
    /// <param name="id">The task ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Task details</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDetailsDto>> GetById(Guid id, CancellationToken ct)
    {
        var task = await _service.GetByIdAsync(id, ct);
        if (task == null)
            return NotFound();

        return Ok(task);
    }

    /// <summary>
    /// Creates a new task. Admin only.
    /// </summary>
    /// <param name="dto">Task input data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created task</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TaskDto>> Create([FromBody] TaskInputDto dto, CancellationToken ct)
    {
        try
        {
            var created = await _service.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing task. Admin only.
    /// </summary>
    /// <param name="id">The task ID</param>
    /// <param name="dto">Updated task data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TaskInputDto dto, CancellationToken ct)
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
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Deletes a task. Admin only.
    /// </summary>
    /// <param name="id">The task ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Adds a tag to a task. Admin only.
    /// </summary>
    /// <param name="taskId">The task ID</param>
    /// <param name="tagId">The tag ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("{taskId:guid}/tags/{tagId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddTag(Guid taskId, Guid tagId, CancellationToken ct)
    {
        await _service.AddTagAsync(taskId, tagId, ct);
        return NoContent();
    }

    /// <summary>
    /// Removes a tag from a task. Admin only.
    /// </summary>
    /// <param name="taskId">The task ID</param>
    /// <param name="tagId">The tag ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{taskId:guid}/tags/{tagId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveTag(Guid taskId, Guid tagId, CancellationToken ct)
    {
        await _service.RemoveTagAsync(taskId, tagId, ct);
        return NoContent();
    }

    /// <summary>
    /// Adds a hint to a task. Admin only.
    /// </summary>
    /// <param name="taskId">The task ID</param>
    /// <param name="hintId">The hint ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("{taskId:guid}/hints/{hintId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddHint(Guid taskId, Guid hintId, CancellationToken ct)
    {
        await _service.AddHintAsync(taskId, hintId, ct);
        return NoContent();
    }

    /// <summary>
    /// Removes a hint from a task. Admin only.
    /// </summary>
    /// <param name="taskId">The task ID</param>
    /// <param name="hintId">The hint ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{taskId:guid}/hints/{hintId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveHint(Guid taskId, Guid hintId, CancellationToken ct)
    {
        await _service.RemoveHintAsync(taskId, hintId, ct);
        return NoContent();
    }
}
