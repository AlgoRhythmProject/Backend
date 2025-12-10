using AlgoRhythm.Shared.Models.Tasks;

namespace AlgoRhythm.Repositories.Tasks.Interfaces;

public interface IHintRepository
{
    Task<IEnumerable<Hint>> GetByTaskIdAsync(Guid taskId, CancellationToken ct);
    Task<Hint?> GetByIdAsync(Guid id, CancellationToken ct);
    Task CreateAsync(Hint hint, CancellationToken ct);
    Task UpdateAsync(Hint hint, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}