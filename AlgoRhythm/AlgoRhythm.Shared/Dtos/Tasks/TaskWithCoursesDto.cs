using AlgoRhythm.Shared.Models.Tasks;
namespace AlgoRhythm.Shared.Dtos.Tasks;

public class TaskWithCoursesDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required Difficulty Difficulty { get; set; }
    public required TaskType TaskType { get; set; } // "Programming" or "Interactive"
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