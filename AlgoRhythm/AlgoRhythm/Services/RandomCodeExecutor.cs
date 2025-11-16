using AlgoRhythm.Api.Dtos;
using AlgoRhythm.Api.Services.Interfaces;
using AlgoRhythm.Repositories.Interfaces;

public class RandomCodeExecutor : ICodeExecutor
{
    private readonly ITaskRepository _taskRepo;
    private readonly Random _rnd = Random.Shared;

    public RandomCodeExecutor(ITaskRepository taskRepo)
    {
        _taskRepo = taskRepo;
    }

    public async Task<IReadOnlyList<TestResultDto>> EvaluateAsync(
        Guid submissionId, Guid taskItemId, string code, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetProgrammingTaskAsync(taskItemId, ct)
            ?? throw new InvalidOperationException("Task not found");
        Thread.Sleep(10000);
        var results = new List<TestResultDto>();

        foreach (var tc in task.TestCases)
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
