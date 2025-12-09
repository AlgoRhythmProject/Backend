using AlgoRhythm.Repositories.Interfaces;
using AlgoRhythm.Services.Interfaces;
using AlgoRhythm.Shared.Dtos.Submissions;
using AlgoRhythm.Shared.Models.CodeExecution.Requests;
using AlgoRhythm.Shared.Models.Tasks;
using System.Text;
using AlgoRhythm.Clients;

namespace AlgoRhythm.Services;

public class CodeExecutor : ICodeExecutor
{
    private readonly ITaskRepository _taskRepository;
    private readonly ILogger<CodeExecutor> _logger;
    private readonly CodeExecutorClient _codeExecutorClient;

    public CodeExecutor(ITaskRepository taskRepo, ILogger<CodeExecutor> logger, CodeExecutorClient client)
    {
        _taskRepository = taskRepo;
        _logger = logger;
        _codeExecutorClient = client;
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

        var results = await _codeExecutorClient.ExecuteAsync(executeCodeRequests);

        return results ?? [];
    }
}
