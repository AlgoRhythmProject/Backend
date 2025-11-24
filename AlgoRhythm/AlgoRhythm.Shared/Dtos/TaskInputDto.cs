using AlgoRhythm.Shared.Models.Tasks;

namespace AlgoRhythm.Shared.Dtos;

public class TaskInputDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public Difficulty Difficulty { get; set; }
    public bool IsPublished { get; set; }
}
