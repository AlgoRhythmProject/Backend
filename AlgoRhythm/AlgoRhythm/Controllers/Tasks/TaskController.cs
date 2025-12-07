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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskDto>>> GetAll(CancellationToken ct)
    {
        var tasks = await _service.GetAllAsync(ct);
        return Ok(tasks);
    }

    [HttpGet("with-courses")]
    public async Task<ActionResult<IEnumerable<TaskWithCoursesDto>>> GetAllWithCourses(CancellationToken ct)
    {
        var tasks = await _service.GetAllWithCoursesAsync(ct);
        return Ok(tasks);
    }

    [HttpGet("published")]
    public async Task<ActionResult<IEnumerable<TaskDto>>> GetPublished(CancellationToken ct)
    {
        var tasks = await _service.GetPublishedAsync(ct);
        return Ok(tasks);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDetailsDto>> GetById(Guid id, CancellationToken ct)
    {
        var task = await _service.GetByIdAsync(id, ct);
        if (task == null)
            return NotFound();

        return Ok(task);
    }

    [HttpPost]
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

    [HttpPut("{id:guid}")]
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{taskId:guid}/tags/{tagId:guid}")]
    public async Task<IActionResult> AddTag(Guid taskId, Guid tagId, CancellationToken ct)
    {
        await _service.AddTagAsync(taskId, tagId, ct);
        return NoContent();
    }

    [HttpDelete("{taskId:guid}/tags/{tagId:guid}")]
    public async Task<IActionResult> RemoveTag(Guid taskId, Guid tagId, CancellationToken ct)
    {
        await _service.RemoveTagAsync(taskId, tagId, ct);
        return NoContent();
    }

    [HttpPost("{taskId:guid}/hints/{hintId:guid}")]
    public async Task<IActionResult> AddHint(Guid taskId, Guid hintId, CancellationToken ct)
    {
        await _service.AddHintAsync(taskId, hintId, ct);
        return NoContent();
    }

    [HttpDelete("{taskId:guid}/hints/{hintId:guid}")]
    public async Task<IActionResult> RemoveHint(Guid taskId, Guid hintId, CancellationToken ct)
    {
        await _service.RemoveHintAsync(taskId, hintId, ct);
        return NoContent();
    }
}
