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

    /// <summary>
    /// Returns all submissions (Admin only).
    /// </summary>
    Task<IEnumerable<SubmissionResponseDto>> GetAllSubmissionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns all submissions for a specific user.
    /// </summary>
    Task<IEnumerable<SubmissionResponseDto>> GetSubmissionsByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns all submissions for a specific user and task.
    /// </summary>
    Task<IEnumerable<SubmissionResponseDto>> GetSubmissionsByUserAndTaskAsync(Guid userId, Guid taskId, CancellationToken ct = default);

    /// <summary>
    /// Returns recent submissions with pagination support.
    /// </summary>
    Task<IEnumerable<SubmissionResponseDto>> GetRecentSubmissionsAsync(int page, int pageSize, CancellationToken ct = default);
}
