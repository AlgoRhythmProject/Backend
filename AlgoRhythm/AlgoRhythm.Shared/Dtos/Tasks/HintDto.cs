namespace AlgoRhythm.Shared.Dtos.Tasks;

public class HintDto
{
    public Guid Id { get; set; }
    public Guid TaskItemId { get; set; }
    public required string Content { get; set; }
    public int Order { get; set; }
}