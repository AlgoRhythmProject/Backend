using AlgoRhythm.Api.Dtos;
using AlgoRhythm.Api.Services.Interfaces;
using AlgoRhythm.Repositories.Interfaces;
using AlgoRhythm.Shared.Models.Submissions;
using AlgoRhythm.Shared.Models.Tasks;

namespace AlgoRhythm.Api.Services.Implementations;

public class SubmissionService : ISubmissionService
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly ITaskRepository _tasksRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICodeExecutor _judge;

    private readonly IServiceScopeFactory _scopeFactory;

    public SubmissionService(
        ISubmissionRepository submissions,
        ITaskRepository tasks,
        ICodeExecutor judge,
        IConfiguration config,
        IUserRepository userRepository,
        IServiceScopeFactory scopeFactory
    )
    {
        _submissionRepository = submissions;
        _tasksRepository = tasks;
        _judge = judge;
        _userRepository = userRepository;
        _scopeFactory = scopeFactory;
    }

    public async Task<SubmissionResponseDto> CreateProgrammingSubmissionAsync(Guid userId, SubmitProgrammingRequest request, CancellationToken ct = default)
    {
        // Validate user & task exist
        var user = await _userRepository.GetUserAsync(userId, ct);
        if (user == null) throw new InvalidOperationException("User not found.");

        var task = await _tasksRepository.GetByIdAsync(request.TaskId, ct);

        if (task is not ProgrammingTaskItem programmingTask)
            throw new InvalidOperationException("Task is not a programming task");


        var submission = new ProgrammingSubmission
        {
            UserId = userId,
            TaskItemId = task.Id,
            Status = SubmissionStatus.Pending,
            Score = null,
            SubmittedAt = DateTime.UtcNow,
            Code = request.Code,
            ExecuteStartedAt = DateTime.UtcNow
        };

        await _submissionRepository.AddSubmissionAsync(submission, ct);


        StartBackgroundEvaluation(submission.Id, request.Code);
        return MapToDto(submission, Array.Empty<TestResultDto>());

    }

    public async Task<SubmissionResponseDto?> GetSubmissionAsync(Guid submissionId, CancellationToken ct = default)
    {
        var submission = await _submissionRepository.GetSubmissionAsync(submissionId, ct);

        if (submission == null) return null;

        var dtos = submission.TestResults.Select(tr => new TestResultDto
        {
            TestCaseId = tr.TestCaseId,
            Passed = tr.Passed,
            Points = tr.Points,
            ExecutionTimeMs = tr.ExecutionTimeMs,
            StdOut = tr.StdOut,
            StdErr = tr.StdErr
        }).ToList();

        return MapToDto(submission, dtos);
    }
    private void StartBackgroundEvaluation(Guid submissionId, string code)
    {
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();

            var submissionRepo = scope.ServiceProvider.GetRequiredService<ISubmissionRepository>();
            var taskRepo = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
            var judge = scope.ServiceProvider.GetRequiredService<ICodeExecutor>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SubmissionService>>();

            try
            {
                var submission = await submissionRepo.GetSubmissionAsync(submissionId, CancellationToken.None);
                if (submission == null)
                    return;

                var task = await taskRepo.GetByIdAsync(submission.TaskItemId, CancellationToken.None);
                if (task is not ProgrammingTaskItem programmingTask)
                    throw new InvalidOperationException("Task is not a programming task");

                submission.Status = SubmissionStatus.Pending;
                await submissionRepo.UpdateSubmissionAsync(submission, CancellationToken.None);

                var service = new SubmissionService(
                    submissionRepo,
                    taskRepo,
                    judge,
                    new ConfigurationBuilder().Build(),
                    scope.ServiceProvider.GetRequiredService<IUserRepository>(),
                    _scopeFactory
                );

                var results = await service.EvaluateAndSaveResultsAsync(submission, programmingTask, code);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background evaluation failed.");
                await submissionRepo.MarkSubmissionAsErrorAsync(submissionId, CancellationToken.None);
            }
        });
    }

    public async Task<IReadOnlyList<TestResultDto>> EvaluateAndSaveResultsAsync(ProgrammingSubmission submission, ProgrammingTaskItem task, string code, CancellationToken ct = default)
    {
        submission.ExecuteStartedAt = submission.ExecuteStartedAt == default ? DateTime.UtcNow : submission.ExecuteStartedAt;

        await _submissionRepository.SaveChangesAsync(ct);

        var judgeResults = (await _judge.EvaluateAsync(submission.Id, task.Id, code, ct)).ToList();

        var orderedTestCases = task.TestCases.OrderBy(tc => tc.Id).ToList();
        var finalResults = new List<TestResultDto>();

        for (int i = 0; i < orderedTestCases.Count; i++)
        {
            var tc = orderedTestCases[i];
            TestResultDto judgeDto;
            if (i < judgeResults.Count)
                judgeDto = judgeResults[i];
            else
                judgeDto = new TestResultDto { TestCaseId = tc.Id, Passed = false, Points = 0, ExecutionTimeMs = 0 };

            var points = Math.Min(tc.MaxPoints, judgeDto.Points);
            var tr = new TestResult
            {
                SubmissionId = submission.Id,
                TestCaseId = tc.Id,
                Passed = judgeDto.Passed,
                Points = points,
                ExecutionTimeMs = judgeDto.ExecutionTimeMs,
                StdOut = judgeDto.StdOut,
                StdErr = judgeDto.StdErr
            };

            await _submissionRepository.AddTestResultAsync(tr, ct);

            finalResults.Add(new TestResultDto
            {
                TestCaseId = tc.Id,
                Passed = tr.Passed,
                Points = tr.Points,
                ExecutionTimeMs = tr.ExecutionTimeMs,
                StdOut = tr.StdOut,
                StdErr = tr.StdErr
            });
        }

        submission.ExecuteFinishedAt = DateTime.UtcNow;
        submission.TestResults = submission.TestResults ?? new List<TestResult>();
        await _submissionRepository.SaveChangesAsync(ct);

        var totalPoints = finalResults.Sum(r => r.Points);
        var maxPoints = task.TestCases.Sum(tc => tc.MaxPoints);
        var score = maxPoints > 0 ? (double)totalPoints / maxPoints * 100.0 : 0;
        submission.Score = Math.Round(score, 2);
        submission.IsSolved = finalResults.All(r => r.Passed);
        submission.Status = submission.IsSolved ? SubmissionStatus.Accepted : SubmissionStatus.Rejected;

        await _submissionRepository.SaveChangesAsync(ct);

        return finalResults;
    }

    private SubmissionResponseDto MapToDto(ProgrammingSubmission submission, IReadOnlyList<TestResultDto> results)
    {
        return new SubmissionResponseDto
        {
            SubmissionId = submission.Id,
            TaskItemId = submission.TaskItemId,
            UserId = submission.UserId,
            Status = submission.Status.ToString(),
            Score = submission.Score,
            IsSolved = submission.IsSolved,
            SubmittedAt = submission.SubmittedAt,
            TestResults = results.ToList()
        };
    }
}
