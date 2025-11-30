using AlgoRhythm.Shared.Dtos.Tasks;

namespace AlgoRhythm.Services.Tasks.Interfaces;

public interface IHintService
{
    Task<IEnumerable<HintDto>> GetByTaskIdAsync(Guid taskId, CancellationToken ct);
    Task<HintDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<HintDto> CreateAsync(HintInputDto dto, CancellationToken ct);
    Task UpdateAsync(Guid id, HintInputDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}