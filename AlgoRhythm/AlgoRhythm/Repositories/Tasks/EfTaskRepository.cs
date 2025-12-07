using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Tasks.Interfaces;
using AlgoRhythm.Shared.Models.Common;
using AlgoRhythm.Shared.Models.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories.Tasks;

public class EfTaskRepository : ITaskRepository
{
    private readonly ApplicationDbContext _db;

    public EfTaskRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<TaskItem>> GetAllAsync(CancellationToken ct)
    {
        return await _db.Set<TaskItem>()
            .Include(t => t.Tags)
            .Include(t => t.Courses)
            .Include(t => t.Hints)
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<TaskItem>> GetPublishedAsync(CancellationToken ct)
    {
        return await _db.Set<TaskItem>()
            .Include(t => t.Tags)
            .Include(t => t.Courses.Where(c => c.IsPublished))
            .Include(t => t.Hints)
            .Where(t => t.IsPublished && !t.IsDeleted)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Set<TaskItem>()
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);
    }

    public async Task<TaskItem?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct)
    {
        return await _db.Set<TaskItem>()
            .Include(t => t.Tags)
            .Include(t => t.Courses)
            .Include(t => t.Hints.OrderBy(h => h.Order))
            .Include(t => t.Comments.Where(c => !c.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);
    }

    public async Task CreateAsync(TaskItem task, CancellationToken ct)
    {
        await _db.Set<TaskItem>().AddAsync(task, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken ct)
    {
        _db.Set<TaskItem>().Update(task);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var task = await _db.Set<TaskItem>().FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task != null)
        {
            task.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task AddTagToTaskAsync(Guid taskId, Guid tagId, CancellationToken ct)
    {
        var task = await _db.Set<TaskItem>()
            .Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

        var tag = await _db.Set<Tag>().FirstOrDefaultAsync(t => t.Id == tagId, ct);

        if (task != null && tag != null && !task.Tags.Contains(tag))
        {
            task.Tags.Add(tag);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task RemoveTagFromTaskAsync(Guid taskId, Guid tagId, CancellationToken ct)
    {
        var task = await _db.Set<TaskItem>()
            .Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

        var tag = task?.Tags.FirstOrDefault(t => t.Id == tagId);

        if (task != null && tag != null)
        {
            task.Tags.Remove(tag);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task AddHintToTaskAsync(Guid taskId, Guid hintId, CancellationToken ct)
    {
        var task = await _db.Set<TaskItem>()
            .Include(t => t.Hints)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

        var hint = await _db.Set<Hint>().FirstOrDefaultAsync(h => h.Id == hintId, ct);

        if (task != null && hint != null && !task.Hints.Contains(hint))
        {
            task.Hints.Add(hint);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task RemoveHintFromTaskAsync(Guid taskId, Guid hintId, CancellationToken ct)
    {
        var task = await _db.Set<TaskItem>()
            .Include(t => t.Hints)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

        var hint = task?.Hints.FirstOrDefault(h => h.Id == hintId);

        if (task != null && hint != null)
        {
            task.Hints.Remove(hint);
            await _db.SaveChangesAsync(ct);
        }
    }
}
