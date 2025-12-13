using AlgoRhythm.Shared.Models.Common;

namespace AlgoRhythm.Repositories.Common.Interfaces;

public interface ICommentRepository
{
    Task<IEnumerable<Comment>> GetByTaskIdAsync(Guid taskId, CancellationToken ct);
    Task<Comment?> GetByIdAsync(Guid id, CancellationToken ct);
    Task CreateAsync(Comment comment, CancellationToken ct);
    Task UpdateAsync(Comment comment, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}