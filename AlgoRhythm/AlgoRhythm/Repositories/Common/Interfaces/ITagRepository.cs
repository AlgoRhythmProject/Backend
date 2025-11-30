using AlgoRhythm.Shared.Models.Common;

namespace AlgoRhythm.Repositories.Common.Interfaces;

public interface ITagRepository
{
    Task<IEnumerable<Tag>> GetAllAsync(CancellationToken ct);
    Task<Tag?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Tag?> GetByNameAsync(string name, CancellationToken ct);
    Task<IEnumerable<Tag>> GetByNamesAsync(IEnumerable<string> names, CancellationToken ct);
    Task CreateAsync(Tag tag, CancellationToken ct);
    Task UpdateAsync(Tag tag, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}