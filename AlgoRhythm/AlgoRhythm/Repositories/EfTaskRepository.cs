using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Interfaces;
using AlgoRhythm.Shared.Models.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories;

public class EfTaskRepository : ITaskRepository
{
    private readonly ApplicationDbContext _db;
    public EfTaskRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<TaskItem>> GetAllAsync(CancellationToken ct)
    {
        return await _db.TaskItems
            .Where(t => !t.IsDeleted)
            .ToListAsync(ct);
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.TaskItems
            .Include(t => (t as ProgrammingTaskItem)!.TestCases)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);
    }

    public async Task CreateAsync(TaskItem task, CancellationToken ct)
    {
        await _db.TaskItems.AddAsync(task, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken ct)
    {
        _db.TaskItems.Update(task);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var task = await _db.TaskItems.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task != null)
        {
            task.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
