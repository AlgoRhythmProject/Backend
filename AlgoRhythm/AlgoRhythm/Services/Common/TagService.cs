using AlgoRhythm.Repositories.Common.Interfaces;
using AlgoRhythm.Services.Common.Interfaces;
using AlgoRhythm.Shared.Dtos.Common;
using AlgoRhythm.Shared.Models.Common;

namespace AlgoRhythm.Services.Common;

public class TagService : ITagService
{
    private readonly ITagRepository _repo;

    public TagService(ITagRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<TagDto>> GetAllAsync(CancellationToken ct)
    {
        var tags = await _repo.GetAllAsync(ct);
        return tags.Select(MapToDto);
    }

    public async Task<TagDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var tag = await _repo.GetByIdAsync(id, ct);
        return tag == null ? null : MapToDto(tag);
    }

    public async Task<TagDto?> GetByNameAsync(string name, CancellationToken ct)
    {
        var tag = await _repo.GetByNameAsync(name, ct);
        return tag == null ? null : MapToDto(tag);
    }

    public async Task<TagDto> CreateAsync(TagInputDto dto, CancellationToken ct)
    {
        // Check if tag with same name already exists
        var existing = await _repo.GetByNameAsync(dto.Name, ct);
        if (existing != null)
            throw new InvalidOperationException($"Tag with name '{dto.Name}' already exists");

        var tag = new Tag
        {
            Name = dto.Name,
            Description = dto.Description
        };

        await _repo.CreateAsync(tag, ct);
        return MapToDto(tag);
    }

    public async Task UpdateAsync(Guid id, TagInputDto dto, CancellationToken ct)
    {
        var tag = await _repo.GetByIdAsync(id, ct);
        if (tag == null)
            throw new KeyNotFoundException("Tag not found");

        // Check if another tag with the same name exists
        var existing = await _repo.GetByNameAsync(dto.Name, ct);
        if (existing != null && existing.Id != id)
            throw new InvalidOperationException($"Tag with name '{dto.Name}' already exists");

        tag.Name = dto.Name;
        tag.Description = dto.Description;

        await _repo.UpdateAsync(tag, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, ct);
    }

    private static TagDto MapToDto(Tag tag)
    {
        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Description = tag.Description
        };
    }
}