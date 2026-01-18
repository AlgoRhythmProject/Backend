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

    /// <summary>
    /// Gets all lectures.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of all lectures</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LectureDto>>> GetAll(CancellationToken ct)
    {
        var lectures = await _service.GetAllAsync(ct);
        return Ok(lectures);
    }

    /// <summary>
    /// Gets all lectures for a specific course.
    /// </summary>
    /// <param name="courseId">The course ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of lectures for the course</returns>
    [HttpGet("course/{courseId:guid}")]
    public async Task<ActionResult<IEnumerable<LectureDto>>> GetByCourseId(Guid courseId, CancellationToken ct)
    {
        var lectures = await _service.GetByCourseIdAsync(courseId, ct);
        return Ok(lectures);
    }

    /// <summary>
    /// Gets a lecture by its ID.
    /// </summary>
    /// <param name="id">The lecture ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Lecture details</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LectureDto>> GetById(Guid id, CancellationToken ct)
    {
        var lecture = await _service.GetByIdAsync(id, ct);
        if (lecture == null)
            return NotFound();

        return Ok(lecture);
    }

    /// <summary>
    /// Creates a new lecture. Admin only.
    /// Lecture is created independently and can be assigned to courses later.
    /// </summary>
    /// <param name="dto">Lecture input data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created lecture</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LectureDto>> Create([FromBody] LectureInputDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates an existing lecture. Admin only.
    /// </summary>
    /// <param name="id">The lecture ID</param>
    /// <param name="dto">Updated lecture data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
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
            return NotFound();
        }
    }

    /// <summary>
    /// Deletes a lecture. Admin only.
    /// </summary>
    /// <param name="id">The lecture ID</param>
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
    /// Adds a tag to a lecture. Admin only.
    /// </summary>
    /// <param name="lectureId">The lecture ID</param>
    /// <param name="tagId">The tag ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPost("{lectureId:guid}/tags/{tagId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddTag(Guid lectureId, Guid tagId, CancellationToken ct)
    {
        await _service.AddTagAsync(lectureId, tagId, ct);
        return NoContent();
    }

    /// <summary>
    /// Removes a tag from a lecture. Admin only.
    /// </summary>
    /// <param name="lectureId">The lecture ID</param>
    /// <param name="tagId">The tag ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{lectureId:guid}/tags/{tagId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveTag(Guid lectureId, Guid tagId, CancellationToken ct)
    {
        await _service.RemoveTagAsync(lectureId, tagId, ct);
        return NoContent();
    }

    /// <summary>
    /// Gets all contents of a lecture.
    /// </summary>
    /// <param name="lectureId">The lecture ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of lecture contents</returns>
    [HttpGet("{lectureId:guid}/contents")]
    public async Task<ActionResult<IEnumerable<LectureContentDto>>> GetAllContents(Guid lectureId, CancellationToken ct)
    {
        var contents = await _service.GetAllContentsByLectureIdAsync(lectureId, ct);
        return Ok(contents);
    }

    /// <summary>
    /// Adds content to a lecture. Admin only.
    /// </summary>
    /// <param name="lectureId">The lecture ID</param>
    /// <param name="dto">Content input data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created content</returns>
    [HttpPost("{lectureId:guid}/contents")]
    [Authorize(Roles = "Admin")]
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

    /// <summary>
    /// Gets a specific lecture content by ID.
    /// </summary>
    /// <param name="lectureId">The lecture ID</param>
    /// <param name="contentId">The content ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Lecture content details</returns>
    [HttpGet("{lectureId:guid}/contents/{contentId:guid}")]
    public async Task<ActionResult<LectureContentDto>> GetContentById(Guid lectureId, Guid contentId, CancellationToken ct)
    {
        var content = await _service.GetContentByIdAsync(contentId, ct);
        if (content == null || content.LectureId != lectureId)
            return NotFound();

        return Ok(content);
    }

    /// <summary>
    /// Updates lecture content. Admin only.
    /// </summary>
    /// <param name="lectureId">The lecture ID</param>
    /// <param name="contentId">The content ID</param>
    /// <param name="dto">Updated content data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPut("{lectureId:guid}/contents/{contentId:guid}")]
    [Authorize(Roles = "Admin")]
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

    /// <summary>
    /// Removes content from a lecture. Admin only.
    /// </summary>
    /// <param name="lectureId">The lecture ID</param>
    /// <param name="contentId">The content ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{lectureId:guid}/contents/{contentId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveContent(Guid lectureId, Guid contentId, CancellationToken ct)
    {
        await _service.RemoveContentAsync(lectureId, contentId, ct);
        return NoContent();
    }

    /// <summary>
    /// Swaps the order of two lecture contents. Admin only.
    /// </summary>
    /// <param name="lectureId">The lecture ID</param>
    /// <param name="dto">Content IDs to swap</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpPatch("{lectureId:guid}/contents/swap-order")]
    [Authorize(Roles = "Admin")]
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