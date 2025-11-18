using AlgoRhythm.Api.Dtos;
using AlgoRhythm.Api.Services.Interfaces;
using AlgoRhythm.Repositories.Interfaces;
using AlgoRhythm.Shared.Models.Tasks;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

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
        ParsedFunction parsedFunction,
        CancellationToken ct = default)
    {
        LogParsedFunction(submissionId, parsedFunction);

        var task = await _taskRepository.GetByIdAsync(taskItemId, ct);
        if (task is not ProgrammingTaskItem programmingTask)
            throw new InvalidOperationException("Task is not a programming task");

        Thread.Sleep(10000);

        var results = new List<TestResultDto>();

        foreach (var tc in programmingTask.TestCases)
        {
            LogTestCaseArguments(tc, parsedFunction);

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

    private void LogParsedFunction(Guid submissionId, ParsedFunction function)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Received function for execution:");
        sb.AppendLine($"Submission ID: {submissionId}");
        sb.AppendLine($"Return Type:   {function.ReturnType}");
        sb.AppendLine($"Name:          {function.FunctionName}");

        sb.AppendLine("Arguments:");
        if (function.Arguments.Any())
        {
            foreach (var arg in function.Arguments)
                sb.AppendLine($"  - {arg.Type} {arg.Name}");
        }
        else
        {
            sb.AppendLine("  (none)");
        }

        sb.AppendLine("Body:");
        sb.AppendLine(function.Body);

        _logger.LogInformation(sb.ToString());
    }

    private void LogTestCaseArguments(TestCase tc, ParsedFunction function)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"TestCase {tc.Id}");
        sb.AppendLine($"InputJson: {tc.InputJson}");

        if (tc.InputJson == null)
        {
            sb.AppendLine("No InputJson provided.");
            _logger.LogInformation(sb.ToString());
            return;
        }

        try
        {
            var inputDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(tc.InputJson);

            sb.AppendLine("Mapped arguments:");
            foreach (var arg in function.Arguments)
            {
                if (inputDict!.TryGetValue(arg.Name, out var value))
                {
                    sb.AppendLine($"  {arg.Name} ({arg.Type}) = {value}");
                }
                else
                {
                    sb.AppendLine($"Missing argument '{arg.Name}' in InputJson");
                }
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine("Failed to parse InputJson:");
            sb.AppendLine(ex.ToString());
        }

        _logger.LogInformation(sb.ToString());
    }
}
