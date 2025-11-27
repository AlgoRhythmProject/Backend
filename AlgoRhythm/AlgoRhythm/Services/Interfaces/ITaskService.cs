using AlgoRhythm.Shared.Dtos.Tasks;

namespace AlgoRhythm.Services.Interfaces;

public interface ITaskService
    {
        Task<IEnumerable<TaskDto>> GetAllAsync(CancellationToken ct);
        Task<TaskDto?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<TaskDto> CreateProgrammingAsync(ProgrammingTaskInputDto task, CancellationToken ct);
        Task UpdateProgrammingAsync(Guid id, ProgrammingTaskInputDto updated, CancellationToken ct);

        Task DeleteAsync(Guid id, CancellationToken ct);
    }

