using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Submissions.Interfaces;
using AlgoRhythm.Shared.Models.Submissions;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories.Submissions;

public class EfSubmissionRepository : ISubmissionRepository
{
    private readonly ApplicationDbContext _context;

    public EfSubmissionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProgrammingSubmission?> GetSubmissionAsync(Guid id, CancellationToken ct)
    {
        return await _context.ProgrammingSubmissions
            .Include(s => s.TestResults)
                .ThenInclude(tr => tr.Errors)
            .Include(s => s.TestResults)
                .ThenInclude(tr => tr.TestCase)
            .Include(s => s.TaskItem)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<IEnumerable<ProgrammingSubmission>> GetAllSubmissionsAsync(CancellationToken ct)
    {
        return await _context.ProgrammingSubmissions
            .Include(s => s.TestResults)
                .ThenInclude(tr => tr.Errors)
            .Include(s => s.TestResults)
                .ThenInclude(tr => tr.TestCase)
            .Include(s => s.TaskItem)
            .Include(s => s.User)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<ProgrammingSubmission>> GetSubmissionsByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await _context.ProgrammingSubmissions
            .Include(s => s.TestResults)
                .ThenInclude(tr => tr.Errors)
            .Include(s => s.TestResults)
                .ThenInclude(tr => tr.TestCase)
            .Include(s => s.TaskItem)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<ProgrammingSubmission>> GetSubmissionsByUserAndTaskAsync(Guid userId, Guid taskId, CancellationToken ct)
    {
        return await _context.ProgrammingSubmissions
            .Include(s => s.TestResults)
                .ThenInclude(tr => tr.Errors)
            .Include(s => s.TestResults)
                .ThenInclude(tr => tr.TestCase)
            .Include(s => s.TaskItem)
            .Where(s => s.UserId == userId && s.TaskItemId == taskId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<ProgrammingSubmission>> GetRecentSubmissionsAsync(int skip, int take, CancellationToken ct)
    {
        return await _context.ProgrammingSubmissions
            .Include(s => s.TestResults)
                .ThenInclude(tr => tr.Errors)
            .Include(s => s.TestResults)
                .ThenInclude(tr => tr.TestCase)
            .Include(s => s.TaskItem)
            .Include(s => s.User)
            .OrderByDescending(s => s.SubmittedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task AddSubmissionAsync(ProgrammingSubmission submission, CancellationToken ct)
    {
        await _context.ProgrammingSubmissions.AddAsync(submission, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateSubmissionAsync(ProgrammingSubmission submission, CancellationToken ct)
    {
        _context.ProgrammingSubmissions.Update(submission);
        await _context.SaveChangesAsync(ct);
    }

    public async Task MarkSubmissionAsErrorAsync(Guid id, CancellationToken ct)
    {
        var submission = await _context.ProgrammingSubmissions.FindAsync([id], ct);
        if (submission != null)
        {
            submission.Status = SubmissionStatus.Error;
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task AddTestResultAsync(TestResult result, CancellationToken ct)
    {
        await _context.TestResults.AddAsync(result, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateSubmissionAfterEvaluationAsync(ProgrammingSubmission submission, CancellationToken ct)
    {
        _context.ProgrammingSubmissions.Update(submission);
        await _context.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _context.SaveChangesAsync(ct);
    }
}
