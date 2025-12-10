namespace AlgoRhythm.Shared.Dtos.Courses;

public class CourseSummaryDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<LectureSummaryDto> Lectures { get; set; } = [];
    public List<TaskSummaryDto> Tasks { get; set; } = [];
}

public class LectureSummaryDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public List<Guid> TagIds { get; set; } = [];
}

public class TaskSummaryDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public List<Guid> TagIds { get; set; } = [];
}