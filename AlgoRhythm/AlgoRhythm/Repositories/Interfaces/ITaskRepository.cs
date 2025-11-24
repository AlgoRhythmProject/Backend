using AlgoRhythm.Shared.Models.Tasks;

namespace AlgoRhythm.Repositories.Interfaces;

public interface ITaskRepository
{
    Task<IEnumerable<TaskItem>> GetAllAsync(CancellationToken ct);
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct);
    Task CreateAsync(TaskItem task, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task UpdateAsync(TaskItem task, CancellationToken ct);
}