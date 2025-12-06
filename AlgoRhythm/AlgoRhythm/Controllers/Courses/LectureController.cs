using AlgoRhythm.Services.Courses.Interfaces;
using AlgoRhythm.Shared.Dtos.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlgoRhythm.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LectureController : ControllerBase
{
    private readonly ILectureService _service;

    public LectureController(ILectureService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LectureDto>>> GetAll(CancellationToken ct)
    {
        var lectures = await _service.GetAllAsync(ct);
        return Ok(lectures);
    }

    [HttpGet("course/{courseId:guid}")]
    public async Task<ActionResult<IEnumerable<LectureDto>>> GetByCourseId(Guid courseId, CancellationToken ct)
    {
        var lectures = await _service.GetByCourseIdAsync(courseId, ct);
        return Ok(lectures);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LectureDto>> GetById(Guid id, CancellationToken ct)
    {
        var lecture = await _service.GetByIdAsync(id, ct);
        if (lecture == null)
            return NotFound();

        return Ok(lecture);
    }

    [HttpPost]
    public async Task<ActionResult<LectureDto>> Create([FromBody] LectureInputDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] LectureInputDto dto, CancellationToken ct)
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

    [HttpPost("{lectureId:guid}/tags/{tagId:guid}")]
    public async Task<IActionResult> AddTag(Guid lectureId, Guid tagId, CancellationToken ct)
    {
        await _service.AddTagAsync(lectureId, tagId, ct);
        return NoContent();
    }

    [HttpDelete("{lectureId:guid}/tags/{tagId:guid}")]
    public async Task<IActionResult> RemoveTag(Guid lectureId, Guid tagId, CancellationToken ct)
    {
        await _service.RemoveTagAsync(lectureId, tagId, ct);
        return NoContent();
    }

    [HttpGet("{lectureId:guid}/contents")]
    public async Task<ActionResult<IEnumerable<LectureContentDto>>> GetAllContents(Guid lectureId, CancellationToken ct)
    {
        var contents = await _service.GetAllContentsByLectureIdAsync(lectureId, ct);
        return Ok(contents);
    }

    [HttpPost("{lectureId:guid}/contents")]
    public async Task<ActionResult<LectureContentDto>> AddContent(Guid lectureId, [FromBody] LectureContentInputDto dto, CancellationToken ct)
    {
        try
        {
            var content = await _service.AddContentAsync(lectureId, dto, ct);
            return CreatedAtAction(nameof(GetContentById), new { lectureId, contentId = content.Id }, content);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Lecture not found");
        }
    }

    [HttpGet("{lectureId:guid}/contents/{contentId:guid}")]
    public async Task<ActionResult<LectureContentDto>> GetContentById(Guid lectureId, Guid contentId, CancellationToken ct)
    {
        var content = await _service.GetContentByIdAsync(contentId, ct);
        if (content == null || content.LectureId != lectureId)
            return NotFound();

        return Ok(content);
    }

    [HttpPut("{lectureId:guid}/contents/{contentId:guid}")]
    public async Task<IActionResult> UpdateContent(Guid lectureId, Guid contentId, [FromBody] LectureContentInputDto dto, CancellationToken ct)
    {
        try
        {
            await _service.UpdateContentAsync(lectureId, contentId, dto, ct);
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

    [HttpDelete("{lectureId:guid}/contents/{contentId:guid}")]
    public async Task<IActionResult> RemoveContent(Guid lectureId, Guid contentId, CancellationToken ct)
    {
        await _service.RemoveContentAsync(lectureId, contentId, ct);
        return NoContent();
    }

    [HttpPatch("{lectureId:guid}/contents/swap-order")]
    public async Task<IActionResult> SwapContentOrder(Guid lectureId, [FromBody] ChangeContentOrderDto dto, CancellationToken ct)
    {
        try
        {
            await _service.SwapContentOrderAsync(lectureId, dto.FirstContentId, dto.SecondContentId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}