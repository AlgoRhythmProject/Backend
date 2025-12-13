using AlgoRhythm.Services.Tasks.Interfaces;
using AlgoRhythm.Shared.Dtos.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlgoRhythm.Controllers.Tasks;

[ApiController]
[Route("api/[controller]")]
public class HintController : ControllerBase
{
    private readonly IHintService _service;
    private readonly ILogger<HintController> _logger;

    public HintController(IHintService service, ILogger<HintController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("task/{taskId:guid}")]
    public async Task<IActionResult> GetByTask(Guid taskId, CancellationToken ct)
    {
        var hints = await _service.GetByTaskIdAsync(taskId, ct);
        return Ok(hints);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var hint = await _service.GetByIdAsync(id, ct);
        if (hint == null)
            return NotFound(new { error = "Hint not found" });

        return Ok(hint);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] HintInputDto dto, CancellationToken ct)
    {
        try
        {
            var created = await _service.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating hint");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] HintInputDto dto, CancellationToken ct)
    {
        try
        {
            await _service.UpdateAsync(id, dto, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Hint not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hint");
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
}