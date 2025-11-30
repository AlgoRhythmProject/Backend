using AlgoRhythm.Repositories.Tasks.Interfaces;
using AlgoRhythm.Services.Tasks.Interfaces;
using AlgoRhythm.Shared.Dtos.Tasks;
using AlgoRhythm.Shared.Models.Tasks;

namespace AlgoRhythm.Services.Tasks;

public class HintService : IHintService
{
    private readonly IHintRepository _repo;

    public HintService(IHintRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<HintDto>> GetByTaskIdAsync(Guid taskId, CancellationToken ct)
    {
        var hints = await _repo.GetByTaskIdAsync(taskId, ct);
        return hints.Select(MapToDto);
    }

    public async Task<HintDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var hint = await _repo.GetByIdAsync(id, ct);
        return hint == null ? null : MapToDto(hint);
    }

    public async Task<HintDto> CreateAsync(HintInputDto dto, CancellationToken ct)
    {
        var hint = new Hint
        {
            TaskItemId = dto.TaskItemId,
            Content = dto.Content,
            Order = dto.Order
        };

        await _repo.CreateAsync(hint, ct);
        return MapToDto(hint);
    }

    public async Task UpdateAsync(Guid id, HintInputDto dto, CancellationToken ct)
    {
        var hint = await _repo.GetByIdAsync(id, ct);
        if (hint == null)
            throw new KeyNotFoundException("Hint not found");

        hint.Content = dto.Content;
        hint.Order = dto.Order;

        await _repo.UpdateAsync(hint, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, ct);
    }

    private static HintDto MapToDto(Hint hint)
    {
        return new HintDto
        {
            Id = hint.Id,
            TaskItemId = hint.TaskItemId,
            Content = hint.Content,
            Order = hint.Order
        };
    }
}