using AlgoRhythm.Shared.Models.Tasks;

namespace AlgoRhythm.Repositories.Interfaces;

public interface ITaskRepository
{
    Task<ProgrammingTaskItem?> GetProgrammingTaskAsync(Guid id, CancellationToken ct);
}
