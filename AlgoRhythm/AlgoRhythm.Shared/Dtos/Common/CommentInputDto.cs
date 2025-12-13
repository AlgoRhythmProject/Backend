namespace AlgoRhythm.Shared.Dtos.Common;

public class CommentInputDto
{
    public Guid TaskItemId { get; set; }
    public required string Content { get; set; }
}