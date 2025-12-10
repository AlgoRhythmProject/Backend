using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Common.Interfaces;
using AlgoRhythm.Shared.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories.Common;

public class EfCommentRepository : ICommentRepository
{
    private readonly ApplicationDbContext _db;
    
    public EfCommentRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Comment>> GetByTaskIdAsync(Guid taskId, CancellationToken ct)
    {
        return await _db.Comments
            .Include(c => c.Author)
            .Where(c => c.TaskItemId == taskId && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Comment?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Comments
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);
    }

    public async Task CreateAsync(Comment comment, CancellationToken ct)
    {
        await _db.Comments.AddAsync(comment, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Comment comment, CancellationToken ct)
    {
        _db.Comments.Update(comment);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (comment != null)
        {
            comment.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}