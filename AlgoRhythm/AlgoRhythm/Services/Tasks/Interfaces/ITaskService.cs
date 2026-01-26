using AlgoRhythm.Shared.Dtos.Tasks;

namespace AlgoRhythm.Services.Tasks.Interfaces;

public interface ITaskService
{
    Task<IEnumerable<TaskDto>> GetAllAsync(CancellationToken ct);
    Task<IEnumerable<TaskWithCoursesDto>> GetPublishedWithCoursesAsync(CancellationToken ct);
    Task<IEnumerable<TaskDto>> GetPublishedAsync(CancellationToken ct);
    Task<TaskDetailsDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<TaskDto> CreateAsync(TaskInputDto dto, CancellationToken ct);
    Task UpdateAsync(Guid id, TaskInputDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task AddTagAsync(Guid taskId, Guid tagId, CancellationToken ct);
    Task RemoveTagAsync(Guid taskId, Guid tagId, CancellationToken ct);
    Task AddHintAsync(Guid taskId, Guid hintId, CancellationToken ct);
    Task RemoveHintAsync(Guid taskId, Guid hintId, CancellationToken ct);
}

