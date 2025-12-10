namespace AlgoRhythm.Shared.Dtos.Tasks;

public class HintInputDto
{
    public Guid TaskItemId { get; set; }
    public required string Content { get; set; }
    public int Order { get; set; }
}