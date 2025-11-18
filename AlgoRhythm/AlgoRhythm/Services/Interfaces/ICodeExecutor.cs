using AlgoRhythm.Api.Dtos;

namespace AlgoRhythm.Api.Services.Interfaces;

public interface ICodeExecutor
{
    Task<IReadOnlyList<TestResultDto>> EvaluateAsync(Guid submissionId, Guid taskItemId, ParsedFunction parsedFunction, CancellationToken ct = default);
}
