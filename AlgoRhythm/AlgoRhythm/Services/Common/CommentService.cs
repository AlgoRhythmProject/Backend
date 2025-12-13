using AlgoRhythm.Repositories.Common.Interfaces;
using AlgoRhythm.Services.Common.Interfaces;
using AlgoRhythm.Shared.Dtos.Common;
using AlgoRhythm.Shared.Models.Common;

namespace AlgoRhythm.Services.Common;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _repo;

    public CommentService(ICommentRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<CommentDto>> GetByTaskIdAsync(Guid taskId, CancellationToken ct)
    {
        var comments = await _repo.GetByTaskIdAsync(taskId, ct);
        return comments.Select(MapToDto);
    }

    public async Task<CommentDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var comment = await _repo.GetByIdAsync(id, ct);
        return comment == null ? null : MapToDto(comment);
    }

    public async Task<CommentDto> CreateAsync(Guid userId, CommentInputDto dto, CancellationToken ct)
    {
        var comment = new Comment
        {
            AuthorId = userId,
            TaskItemId = dto.TaskItemId,
            Content = dto.Content
        };

        await _repo.CreateAsync(comment, ct);
        
        // Reload with author info
        var created = await _repo.GetByIdAsync(comment.Id, ct);
        return MapToDto(created!);
    }

    public async Task UpdateAsync(Guid id, Guid userId, string content, CancellationToken ct)
    {
        var comment = await _repo.GetByIdAsync(id, ct);
        if (comment == null)
            throw new KeyNotFoundException("Comment not found");

        if (comment.AuthorId != userId)
            throw new UnauthorizedAccessException("You can only edit your own comments");

        comment.Content = content;
        comment.IsEdited = true;
        comment.EditedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(comment, ct);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken ct)
    {
        var comment = await _repo.GetByIdAsync(id, ct);
        if (comment == null)
            throw new KeyNotFoundException("Comment not found");

        if (comment.AuthorId != userId)
            throw new UnauthorizedAccessException("You can only delete your own comments");

        await _repo.DeleteAsync(id, ct);
    }

    private static CommentDto MapToDto(Comment comment)
    {
        return new CommentDto
        {
            Id = comment.Id,
            AuthorId = comment.AuthorId,
            AuthorName = $"{comment.Author?.FirstName} {comment.Author?.LastName}".Trim(),
            TaskItemId = comment.TaskItemId,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            IsEdited = comment.IsEdited,
            EditedAt = comment.EditedAt
        };
    }
}