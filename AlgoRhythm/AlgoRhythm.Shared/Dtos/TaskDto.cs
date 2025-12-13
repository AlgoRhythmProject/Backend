namespace AlgoRhythm.Shared.Dtos;
public class TaskDto
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public required string Difficulty { get; set; }

    public bool IsPublished { get; set; }

    public required string Type { get; set; }
}