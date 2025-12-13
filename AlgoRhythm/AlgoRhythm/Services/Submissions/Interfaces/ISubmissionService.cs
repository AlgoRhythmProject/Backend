using AlgoRhythm.Shared.Dtos.Submissions;

namespace AlgoRhythm.Services.Submissions.Interfaces;

public interface ISubmissionService
{
    /// <summary>
    /// Creates programming submission and either evaluates synchronously or enqueues for background processing.
    /// Returns the created submission id and optionally initial status/result.
    /// </summary>
    Task<SubmissionResponseDto> CreateProgrammingSubmissionAsync(Guid userId, SubmitProgrammingRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Returns submission with test results.
    /// </summary>
    Task<SubmissionResponseDto?> GetSubmissionAsync(Guid submissionId, CancellationToken ct = default);
}
