namespace AlgoRhythm.Shared.Dtos.Tasks;

public class TaskDetailsDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string Difficulty { get; set; }
    public required string TaskType { get; set; } // "Programming" or "Interactive"
    public bool IsPublished { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // For ProgrammingTaskItem
    public string? TemplateCode { get; set; }
    public List<Guid> TestCaseIds { get; set; } = [];
    
    // For InteractiveTaskItem
    public string? OptionsJson { get; set; }
    public string? CorrectAnswer { get; set; }
    
    public List<Guid> TagIds { get; set; } = [];
    public List<Guid> HintIds { get; set; } = [];
    public List<TaskCourseSummaryDto> Courses { get; set; } = [];
}