using AlgoRhythm.Shared.Models.Submissions;

namespace AlgoRhythm.Repositories.Submissions.Interfaces;

public interface ISubmissionRepository
{
    Task<ProgrammingSubmission?> GetSubmissionAsync(Guid id, CancellationToken ct);
    Task AddSubmissionAsync(ProgrammingSubmission submission, CancellationToken ct);
    Task UpdateSubmissionAsync(ProgrammingSubmission submission, CancellationToken ct);
    Task MarkSubmissionAsErrorAsync(Guid id, CancellationToken ct);
    Task AddTestResultAsync(TestResult result, CancellationToken ct);
    Task UpdateSubmissionAfterEvaluationAsync(ProgrammingSubmission submission, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);

}
