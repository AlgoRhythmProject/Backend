using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Tasks.Interfaces;
using AlgoRhythm.Shared.Models.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories.Tasks;

public class EfTestCaseRepository : ITestCaseRepository
{
    private readonly ApplicationDbContext _context;

    public EfTestCaseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TestCase>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.TestCases
            .Include(tc => tc.ProgrammingTaskItem)
            .OrderBy(tc => tc.ProgrammingTaskItemId)
            .ThenBy(tc => tc.MaxPoints)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<TestCase>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default)
    {
        return await _context.TestCases
            .Where(tc => tc.ProgrammingTaskItemId == taskId)
            .OrderBy(tc => tc.MaxPoints)
            .ToListAsync(ct);
    }

    public async Task<TestCase?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.TestCases
            .Include(tc => tc.ProgrammingTaskItem)
            .FirstOrDefaultAsync(tc => tc.Id == id, ct);
    }

    public async Task<TestCase> CreateAsync(TestCase testCase, CancellationToken ct = default)
    {
        _context.TestCases.Add(testCase);
        await _context.SaveChangesAsync(ct);
        return testCase;
    }

    public async Task<TestCase> UpdateAsync(TestCase testCase, CancellationToken ct = default)
    {
        _context.TestCases.Update(testCase);
        await _context.SaveChangesAsync(ct);
        return testCase;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var testCase = await _context.TestCases.FindAsync(new object[] { id }, ct);
        if (testCase != null)
        {
            _context.TestCases.Remove(testCase);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.TestCases.AnyAsync(tc => tc.Id == id, ct);
    }

    public async Task<bool> TaskExistsAsync(Guid taskId, CancellationToken ct = default)
    {
        return await _context.TaskItems
            .OfType<ProgrammingTaskItem>()
            .AnyAsync(t => t.Id == taskId, ct);
    }
}