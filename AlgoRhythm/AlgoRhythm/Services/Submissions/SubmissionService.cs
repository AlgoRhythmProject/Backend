using AlgoRhythm.Repositories.Submissions.Interfaces;
using AlgoRhythm.Repositories.Tasks.Interfaces;
using AlgoRhythm.Services.CodeExecutor.Interfaces;
using AlgoRhythm.Services.Submissions.Interfaces;
using AlgoRhythm.Shared.Dtos.Submissions;
using AlgoRhythm.Shared.Models.CodeExecution.Requests;
using AlgoRhythm.Shared.Models.Submissions;
using AlgoRhythm.Shared.Models.Tasks;
using AlgoRhythm.Shared.Models.Users;
using Microsoft.AspNetCore.Identity;

namespace AlgoRhythm.Services.Submissions;

public class SubmissionService : ISubmissionService
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly ITaskRepository _tasksRepository;
    private readonly ICodeExecutor _judge;
    private readonly UserManager<User> _userManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICodeParser _codeParser;

    public SubmissionService(
        ISubmissionRepository submissions,
        ITaskRepository tasks,
        ICodeExecutor judge,
        IConfiguration config,
        UserManager<User> userManager,
        IServiceScopeFactory scopeFactory,
        ICodeParser codeParser
    )
    {
        _submissionRepository = submissions;
        _tasksRepository = tasks;
        _judge = judge;
        _userManager = userManager;
        _scopeFactory = scopeFactory;
        _codeParser = codeParser;
    }

    public async Task<SubmissionResponseDto> CreateProgrammingSubmissionAsync(Guid userId, SubmitProgrammingRequestDto request, CancellationToken ct = default)
    {
        // 1. Validate user & task exist
        var user = await _userManager.FindByIdAsync(userId.ToString());
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
            ExecuteStartedAt = DateTime.UtcNow,
        };

        await _submissionRepository.AddSubmissionAsync(submission, ct);

        // 2. Parse code & validate arguments against test cases
        List<ExecuteCodeRequest> executeRequests;
        try
        {
            _codeParser.ValidateArguments(request.Code, programmingTask.TestCases);

            executeRequests = _codeParser.BuildRequestsForTestCases(request.Code, programmingTask.TestCases);
        }
        catch (Exception ex)
        {
            submission.Status = SubmissionStatus.Error;
            submission.ExecuteFinishedAt = DateTime.UtcNow;
            submission.Score = 0;
            await _submissionRepository.UpdateSubmissionAsync(submission, ct);

            var dto = MapToDto(submission, Array.Empty<TestResultDto>());
            dto.ErrorMessage = ex.Message;
            return dto;
        }

        // 3. Start background evaluation
        StartBackgroundEvaluation(submission.Id, programmingTask.Id, executeRequests);

        return MapToDto(submission, []);

    }

    private void StartBackgroundEvaluation(
        Guid submissionId,
        Guid taskId,
        List<ExecuteCodeRequest> executeCodeRequests)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var submissionRepo = scope.ServiceProvider.GetRequiredService<ISubmissionRepository>();
                var taskRepo = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                var codeParser = scope.ServiceProvider.GetRequiredService<ICodeParser>();
                var judge = scope.ServiceProvider.GetRequiredService<ICodeExecutor>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<SubmissionService>>();

                logger.LogInformation("Starting background evaluation for submission {SubmissionId}", submissionId);

                var submission = await submissionRepo.GetSubmissionAsync(submissionId, CancellationToken.None);
                if (submission is not ProgrammingSubmission progSubmission)
                {
                    logger.LogWarning("Submission {SubmissionId} not found or not programming submission", submissionId);
                    return;
                }

                var task = await taskRepo.GetByIdAsync(taskId, CancellationToken.None);
                if (task is not ProgrammingTaskItem programmingTask)
                {
                    logger.LogError("Task {TaskId} is not a programming task", taskId);
                    await submissionRepo.MarkSubmissionAsErrorAsync(submissionId, CancellationToken.None);
                    return;
                }

                await EvaluateAndSaveResultsInternalAsync(
                    progSubmission,
                    programmingTask,
                    executeCodeRequests,
                    submissionRepo,
                    judge,
                    logger
                );

                logger.LogInformation("Background evaluation completed for submission {SubmissionId}", submissionId);
            }
            catch (Exception ex)
            {
                using var scope = _scopeFactory.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<SubmissionService>>();
                var submissionRepo = scope.ServiceProvider.GetRequiredService<ISubmissionRepository>();

                logger.LogError(ex, "Background evaluation failed for submission {SubmissionId}", submissionId);

                try
                {
                    await submissionRepo.MarkSubmissionAsErrorAsync(submissionId, CancellationToken.None);
                }
                catch (Exception innerEx)
                {
                    logger.LogError(innerEx, "Failed to mark submission as error");
                }
            }
        });
    }

    private async Task EvaluateAndSaveResultsInternalAsync(
        ProgrammingSubmission submission,
        ProgrammingTaskItem task,
        List<ExecuteCodeRequest> executeCodeRequests,
        ISubmissionRepository submissionRepo,
        ICodeExecutor judge,
        ILogger logger)
    {
        submission.Status = SubmissionStatus.Pending;
        submission.ExecuteStartedAt = DateTime.UtcNow;
        await submissionRepo.UpdateSubmissionAsync(submission, CancellationToken.None);

        var judgeResults = (await judge.EvaluateAsync(
            submission.Id,
            task.Id,
            executeCodeRequests,
            CancellationToken.None
        )).ToList();

        var orderedTestCases = task.TestCases.OrderBy(tc => tc.Id).ToList();
        var finalResults = new List<TestResultDto>();

        for (int i = 0; i < orderedTestCases.Count; i++)
        {
            var tc = orderedTestCases[i];
            TestResultDto judgeDto;

            if (i < judgeResults.Count)
            {
                judgeDto = judgeResults[i];
            }
            else
            {
                judgeDto = new TestResultDto
                {
                    TestCaseId = tc.Id,
                    Passed = false,
                    Points = 0,
                    ExecutionTimeMs = 0
                };
            }

            var points = Math.Min(tc.MaxPoints, judgeDto.Points);
            var tr = new TestResult
            {
                SubmissionId = submission.Id,
                TestCaseId = tc.Id,
                Passed = judgeDto.Passed,
                Points = points,
                ExecutionTimeMs = judgeDto.ExecutionTimeMs,
                StdOut = judgeDto.StdOut,
                StdErr = judgeDto.StdErr,
            };

            await submissionRepo.AddTestResultAsync(tr, CancellationToken.None);

            finalResults.Add(judgeResults[i]);
        }

        submission.ExecuteFinishedAt = DateTime.UtcNow;

        var totalPoints = finalResults.Sum(r => r.Points);
        var maxPoints = task.TestCases.Sum(tc => tc.MaxPoints);
        var score = maxPoints > 0 ? (double)totalPoints / maxPoints * 100.0 : 0;

        submission.Score = Math.Round(score, 2);
        submission.IsSolved = finalResults.All(r => r.Passed);
        submission.Status = finalResults.ToSubmissionStatus();

        await submissionRepo.SaveChangesAsync(CancellationToken.None);
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
        }).ToList() ?? [];


        return MapToDto(submission, dtos);
    }

    private static SubmissionResponseDto MapToDto(ProgrammingSubmission submission, IReadOnlyList<TestResultDto> results)
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
