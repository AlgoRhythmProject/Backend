using AlgoRhythm.Repositories.Interfaces;
using AlgoRhythm.Services.Interfaces;
using AlgoRhythm.Shared.Dtos.Submissions;
using AlgoRhythm.Shared.Models.CodeExecution.Requests;
using AlgoRhythm.Shared.Models.Tasks;
using System.Text;

namespace AlgoRhythm.Services;

public class RandomCodeExecutor : ICodeExecutor
{
    private readonly ITaskRepository _taskRepository;
    private readonly Random _rnd = Random.Shared;
    private readonly ILogger<RandomCodeExecutor> _logger;

    public RandomCodeExecutor(ITaskRepository taskRepo, ILogger<RandomCodeExecutor> logger)
    {
        _taskRepository = taskRepo;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TestResultDto>> EvaluateAsync(
        Guid submissionId,
        Guid taskItemId,
        List<ExecuteCodeRequest> executeCodeRequests,
        CancellationToken ct = default)
    {
        var task = await _taskRepository.GetByIdAsync(taskItemId, ct);
        if (task is not ProgrammingTaskItem programmingTask)
            throw new InvalidOperationException("Task is not a programming task");

        var results = new List<TestResultDto>();

        for (int i = 0; i < executeCodeRequests.Count; i++)
        {
            var request = executeCodeRequests[i];
            var tc = programmingTask.TestCases.ElementAt(i);

            LogExecuteCodeRequest(submissionId, request, tc);

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

    private void LogExecuteCodeRequest(Guid submissionId, ExecuteCodeRequest request, TestCase tc)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Submission ID: {submissionId}");
        sb.AppendLine($"TestCase ID: {tc.Id}");
        sb.AppendLine($"Execution Class: {request.ExecutionClass}");
        sb.AppendLine($"Execution Method: {request.ExecutionMethod}");
        sb.AppendLine("Code:");
        sb.AppendLine(request.Code);

        sb.AppendLine("Arguments:");
        if (request.Args.Any())
        {
            foreach (var arg in request.Args)
                sb.AppendLine($"  {arg.Name} = {arg.Value}");
        }
        else
        {
            sb.AppendLine("  (none)");
        }

        _logger.LogInformation(sb.ToString());
    }
}
