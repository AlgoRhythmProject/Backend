using AlgoRhythm.Api.Dtos;
using AlgoRhythm.Api.Services.Interfaces;
using AlgoRhythm.Repositories.Interfaces;
using AlgoRhythm.Shared.Models.Tasks;

public class RandomCodeExecutor : ICodeExecutor
{
    private readonly ITaskRepository _taskRepository;
    private readonly Random _rnd = Random.Shared;

    public RandomCodeExecutor(ITaskRepository taskRepo)
    {
        _taskRepository = taskRepo;
    }

    public async Task<IReadOnlyList<TestResultDto>> EvaluateAsync(
        Guid submissionId, Guid taskItemId, string code, CancellationToken ct = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskItemId, ct);
        if (task is not ProgrammingTaskItem programmingTask)
            throw new InvalidOperationException("Task is not a programming task");

        Thread.Sleep(10000);
        var results = new List<TestResultDto>();

        foreach (var tc in programmingTask.TestCases)
        {
            var passed = _rnd.NextDouble() > 0.4;

            results.Add(new TestResultDto
            {
                TestCaseId = tc.Id,
                Passed = passed,
                Points = passed ? tc.MaxPoints : 0,
                ExecutionTimeMs = Math.Round(_rnd.NextDouble() * 200, 2),
                StdOut = passed ? "OK" : null,
                StdErr = passed ? null : "Random error"
            });
        }

        return results;
    }
}
