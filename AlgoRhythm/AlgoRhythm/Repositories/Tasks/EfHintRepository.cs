using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Tasks.Interfaces;
using AlgoRhythm.Shared.Models.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories.Tasks;

public class EfHintRepository : IHintRepository
{
    private readonly ApplicationDbContext _db;
    
    public EfHintRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Hint>> GetByTaskIdAsync(Guid taskId, CancellationToken ct)
    {
        return await _db.Hints
            .Where(h => h.TaskItemId == taskId)
            .OrderBy(h => h.Order)
            .ToListAsync(ct);
    }

    public async Task<Hint?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Hints.FirstOrDefaultAsync(h => h.Id == id, ct);
    }

    public async Task CreateAsync(Hint hint, CancellationToken ct)
    {
        await _db.Hints.AddAsync(hint, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Hint hint, CancellationToken ct)
    {
        _db.Hints.Update(hint);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var hint = await _db.Hints.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (hint != null)
        {
            _db.Hints.Remove(hint);
            await _db.SaveChangesAsync(ct);
        }
    }
}