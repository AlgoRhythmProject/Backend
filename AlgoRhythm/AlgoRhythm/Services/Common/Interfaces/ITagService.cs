using AlgoRhythm.Shared.Dtos.Common;

namespace AlgoRhythm.Services.Common.Interfaces;

public interface ITagService
{
    Task<IEnumerable<TagDto>> GetAllAsync(CancellationToken ct);
    Task<TagDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<TagDto?> GetByNameAsync(string name, CancellationToken ct);
    Task<TagDto> CreateAsync(TagInputDto dto, CancellationToken ct);
    Task UpdateAsync(Guid id, TagInputDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}