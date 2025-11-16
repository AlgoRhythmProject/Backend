using AlgoRhythm.Api.Dtos;
using AlgoRhythm.Shared.Models.Submissions;

namespace AlgoRhythm.Api.Services.Interfaces;

public interface ISubmissionService
{
    /// <summary>
    /// Creates programming submission and either evaluates synchronously or enqueues for background processing.
    /// Returns the created submission id and optionally initial status/result.
    /// </summary>
    Task<SubmissionResponseDto> CreateProgrammingSubmissionAsync(Guid userId, SubmitProgrammingRequest request, CancellationToken ct = default);

    /// <summary>
    /// Returns submission with test results.
    /// </summary>
    Task<SubmissionResponseDto?> GetSubmissionAsync(Guid submissionId, CancellationToken ct = default);
}
