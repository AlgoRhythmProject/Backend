using AlgoRhythm.Shared.Models.Tasks;

namespace AlgoRhythm.Shared.Dtos.Tasks;

public enum TaskType
{
    Programming,
    Interactive
}

public class TaskDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required Difficulty Difficulty { get; set; }
    public TaskType TaskType { get; set; }
    public bool IsPublished { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public string? TemplateCode { get; set; }
    
    public string? OptionsJson { get; set; }
    public string? CorrectAnswer { get; set; }
    
    public List<Guid> TagIds { get; set; } = [];
    public List<Guid> HintIds { get; set; } = [];
}