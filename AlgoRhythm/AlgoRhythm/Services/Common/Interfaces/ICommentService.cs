using AlgoRhythm.Shared.Dtos.Common;

namespace AlgoRhythm.Services.Common.Interfaces;

public interface ICommentService
{
    Task<IEnumerable<CommentDto>> GetByTaskIdAsync(Guid taskId, CancellationToken ct);
    Task<CommentDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<CommentDto> CreateAsync(Guid userId, CommentInputDto dto, CancellationToken ct);
    Task UpdateAsync(Guid id, Guid userId, string content, CancellationToken ct);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct);
}