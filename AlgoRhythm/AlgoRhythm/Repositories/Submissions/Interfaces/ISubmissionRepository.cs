using AlgoRhythm.Shared.Models.Submissions;

namespace AlgoRhythm.Repositories.Submissions.Interfaces;

public interface ISubmissionRepository
{
    Task<ProgrammingSubmission?> GetSubmissionAsync(Guid id, CancellationToken ct);
    Task<IEnumerable<ProgrammingSubmission>> GetAllSubmissionsAsync(CancellationToken ct);
    Task<IEnumerable<ProgrammingSubmission>> GetSubmissionsByUserIdAsync(Guid userId, CancellationToken ct);
    Task<IEnumerable<ProgrammingSubmission>> GetSubmissionsByUserAndTaskAsync(Guid userId, Guid taskId, CancellationToken ct);
    Task<IEnumerable<ProgrammingSubmission>> GetRecentSubmissionsAsync(int skip, int take, CancellationToken ct);
    Task AddSubmissionAsync(ProgrammingSubmission submission, CancellationToken ct);
    Task UpdateSubmissionAsync(ProgrammingSubmission submission, CancellationToken ct);
    Task MarkSubmissionAsErrorAsync(Guid id, CancellationToken ct);
    Task AddTestResultAsync(TestResult result, CancellationToken ct);
    Task UpdateSubmissionAfterEvaluationAsync(ProgrammingSubmission submission, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
