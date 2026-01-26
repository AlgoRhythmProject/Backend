using AlgoRhythm.Shared.Models.Tasks;

namespace AlgoRhythm.Repositories.Tasks.Interfaces;

public interface ITaskRepository
{
    Task<IEnumerable<TaskItem>> GetAllAsync(CancellationToken ct);
    Task<IEnumerable<TaskItem>> GetPublishedAsync(bool includeCourses, CancellationToken ct);
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<TaskItem?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct);
    Task CreateAsync(TaskItem task, CancellationToken ct);
    Task UpdateAsync(TaskItem task, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task AddTagToTaskAsync(Guid taskId, Guid tagId, CancellationToken ct);
    Task RemoveTagFromTaskAsync(Guid taskId, Guid tagId, CancellationToken ct);
    Task AddHintToTaskAsync(Guid taskId, Guid hintId, CancellationToken ct);
    Task RemoveHintFromTaskAsync(Guid taskId, Guid hintId, CancellationToken ct);
}