using AlgoRhythm.Shared.Models.Tasks;

public class TaskInputDto
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public Difficulty Difficulty { get; set; }
    public bool IsPublished { get; set; }
}
