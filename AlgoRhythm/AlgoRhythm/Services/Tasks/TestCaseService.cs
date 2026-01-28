using AlgoRhythm.Repositories.Tasks.Interfaces;
using AlgoRhythm.Services.Tasks.Interfaces;
using AlgoRhythm.Shared.Dtos.Tasks;
using AlgoRhythm.Shared.Models.Tasks;

namespace AlgoRhythm.Services.Tasks;

public class TestCaseService : ITestCaseService
{
    private readonly ITestCaseRepository _repo;
    private readonly ILogger<TestCaseService> _logger;

    public TestCaseService(ITestCaseRepository repo, ILogger<TestCaseService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<IEnumerable<TestCaseDto>> GetAllAsync(CancellationToken ct = default)
    {
        var testCases = await _repo.GetAllAsync(ct);
        return testCases.Select(MapToDto);
    }

    public async Task<IEnumerable<TestCaseDto>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default)
    {
        if (!await _repo.TaskExistsAsync(taskId, ct))
        {
            throw new KeyNotFoundException($"Programming task with ID {taskId} not found");
        }

        var testCases = await _repo.GetByTaskIdAsync(taskId, ct);
        return testCases.Select(MapToDto);
    }

    public async Task<TestCaseDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var testCase = await _repo.GetByIdAsync(id, ct);
        if (testCase == null)
        {
            throw new KeyNotFoundException($"Test case with ID {id} not found");
        }

        return MapToDto(testCase);
    }

    public async Task<TestCaseDto> CreateAsync(CreateTestCaseDto dto, CancellationToken ct = default)
    {
        // Validate that the programming task exists
        if (!await _repo.TaskExistsAsync(dto.ProgrammingTaskItemId, ct))
        {
            throw new KeyNotFoundException($"Programming task with ID {dto.ProgrammingTaskItemId} not found");
        }

        var testCase = new TestCase
        {
            ProgrammingTaskItemId = dto.ProgrammingTaskItemId,
            InputJson = dto.InputJson,
            ExpectedJson = dto.ExpectedJson,
            IsVisible = dto.IsVisible,
            MaxPoints = dto.MaxPoints,
            TimeoutMs = dto.TimeoutMs
        };

        var created = await _repo.CreateAsync(testCase, ct);
        _logger.LogInformation("Created test case {Id} for task {TaskId}", created.Id, dto.ProgrammingTaskItemId);

        return MapToDto(created);
    }

    public async Task<TestCaseDto> UpdateAsync(Guid id, UpdateTestCaseDto dto, CancellationToken ct = default)
    {
        var testCase = await _repo.GetByIdAsync(id, ct);
        if (testCase == null)
        {
            throw new KeyNotFoundException($"Test case with ID {id} not found");
        }

        testCase.InputJson = dto.InputJson;
        testCase.ExpectedJson = dto.ExpectedJson;
        testCase.IsVisible = dto.IsVisible;
        testCase.MaxPoints = dto.MaxPoints;
        testCase.TimeoutMs = dto.TimeoutMs;

        var updated = await _repo.UpdateAsync(testCase, ct);
        _logger.LogInformation("Updated test case {Id}", id);

        return MapToDto(updated);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!await _repo.ExistsAsync(id, ct))
        {
            throw new KeyNotFoundException($"Test case with ID {id} not found");
        }

        await _repo.DeleteAsync(id, ct);
        _logger.LogInformation("Deleted test case {Id}", id);
    }

    private static TestCaseDto MapToDto(TestCase testCase)
    {
        return new TestCaseDto
        {
            Id = testCase.Id,
            ProgrammingTaskItemId = testCase.ProgrammingTaskItemId,
            InputJson = testCase.InputJson,
            ExpectedJson = testCase.ExpectedJson,
            IsVisible = testCase.IsVisible,
            MaxPoints = testCase.MaxPoints,
            TimeoutMs = testCase.TimeoutMs
        };
    }
}