using AlgoRhythm.Shared.Dtos.Submissions;
using AlgoRhythm.Shared.Models.CodeExecution.Requests;

namespace AlgoRhythm.Services.CodeExecutor.Interfaces;

public interface ICodeExecutor
{
    Task<IReadOnlyList<TestResultDto>> EvaluateAsync(Guid submissionId, Guid taskItemId, List<ExecuteCodeRequest> executeCodeRequests, CancellationToken ct = default);
}
