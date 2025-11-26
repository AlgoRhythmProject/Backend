using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Interfaces;
using AlgoRhythm.Shared.Models.Submissions;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories;
public class EfSubmissionRepository : ISubmissionRepository
{
    private readonly ApplicationDbContext _db;

    public EfSubmissionRepository(ApplicationDbContext db) => _db = db;

    public async Task<ProgrammingSubmission?> GetSubmissionAsync(Guid id, CancellationToken ct)
        => await _db.ProgrammingSubmissions
            .Include(s => s.TestResults)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task AddSubmissionAsync(ProgrammingSubmission submission, CancellationToken ct)
    {
        _db.ProgrammingSubmissions.Add(submission);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateSubmissionAsync(ProgrammingSubmission submission, CancellationToken ct)
    {
        _db.ProgrammingSubmissions.Update(submission);
        await _db.SaveChangesAsync(ct);
    }


    public async Task MarkSubmissionAsErrorAsync(Guid id, CancellationToken ct)
    {
        var s = await _db.ProgrammingSubmissions.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (s == null) return;

        s.Status = SubmissionStatus.Error;
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddTestResultAsync(TestResult result, CancellationToken ct)
    {
        _db.Set<TestResult>().Add(result);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateSubmissionAfterEvaluationAsync(ProgrammingSubmission submission, CancellationToken ct)
    {
        _db.ProgrammingSubmissions.Update(submission);
        await _db.SaveChangesAsync(ct);
    }
    public Task SaveChangesAsync(CancellationToken ct)
    => _db.SaveChangesAsync(ct);

}
