using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Common.Interfaces;
using AlgoRhythm.Shared.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories.Common;

public class EfTagRepository : ITagRepository
{
    private readonly ApplicationDbContext _db;
    
    public EfTagRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Tag>> GetAllAsync(CancellationToken ct)
    {
        return await _db.Tags
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<Tag?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<Tag?> GetByNameAsync(string name, CancellationToken ct)
    {
        return await _db.Tags.FirstOrDefaultAsync(t => t.Name == name, ct);
    }

    public async Task<IEnumerable<Tag>> GetByNamesAsync(IEnumerable<string> names, CancellationToken ct)
    {
        return await _db.Tags
            .Where(t => names.Contains(t.Name))
            .ToListAsync(ct);
    }

    public async Task CreateAsync(Tag tag, CancellationToken ct)
    {
        await _db.Tags.AddAsync(tag, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Tag tag, CancellationToken ct)
    {
        _db.Tags.Update(tag);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tag != null)
        {
            _db.Tags.Remove(tag);
            await _db.SaveChangesAsync(ct);
        }
    }
}