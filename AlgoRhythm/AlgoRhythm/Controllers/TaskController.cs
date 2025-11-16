using AlgoRhythm.Services;
using Microsoft.AspNetCore.Mvc;

namespace AlgoRhythm.Controllers;

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
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        return Ok(await _service.GetAllAsync(ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var task = await _service.GetByIdAsync(id, ct);

        if (task == null)
            return NotFound();

        return Ok(task);
    }

    [HttpPost("programming")]
    public async Task<IActionResult> CreateProgramming([FromBody] ProgrammingTaskInputDto dto, CancellationToken ct)
    {
        var created = await _service.CreateProgrammingAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("programming/{id:guid}")]
    public async Task<IActionResult> UpdateProgramming(Guid id, [FromBody] ProgrammingTaskInputDto dto, CancellationToken ct)
    {
        await _service.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
