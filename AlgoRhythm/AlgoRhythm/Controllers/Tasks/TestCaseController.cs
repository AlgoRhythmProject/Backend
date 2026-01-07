using AlgoRhythm.Services.Tasks.Interfaces;
using AlgoRhythm.Shared.Dtos.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlgoRhythm.Controllers.Tasks;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class TestCaseController : ControllerBase
{
    private readonly ITestCaseService _service;
    private readonly ILogger<TestCaseController> _logger;

    public TestCaseController(ITestCaseService service, ILogger<TestCaseController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all test cases (Admin only)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TestCaseDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var testCases = await _service.GetAllAsync(ct);
        return Ok(testCases);
    }

    /// <summary>
    /// Get all test cases for a specific programming task (Admin only)
    /// </summary>
    [HttpGet("task/{taskId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<TestCaseDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByTaskId(Guid taskId, CancellationToken ct)
    {
        try
        {
            var testCases = await _service.GetByTaskIdAsync(taskId, ct);
            return Ok(testCases);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific test case by ID (Admin only)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TestCaseDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var testCase = await _service.GetByIdAsync(id, ct);
            return Ok(testCase);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new test case (Admin only)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TestCaseDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Create([FromBody] CreateTestCaseDto dto, CancellationToken ct)
    {
        try
        {
            var testCase = await _service.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = testCase.Id }, testCase);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing test case (Admin only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TestCaseDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTestCaseDto dto, CancellationToken ct)
    {
        try
        {
            var testCase = await _service.UpdateAsync(id, dto, ct);
            return Ok(testCase);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a test case (Admin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _service.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}