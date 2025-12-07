namespace AlgoRhythm.Shared.Dtos.Tasks;

public class TaskWithCoursesDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string Difficulty { get; set; }
    public required string TaskType { get; set; } // "Programming" or "Interactive"
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public List<Guid> TagIds { get; set; } = [];
    public List<TaskCourseSummaryDto> Courses { get; set; } = [];
}

public class TaskCourseSummaryDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
}