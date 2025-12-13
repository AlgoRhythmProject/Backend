namespace AlgoRhythm.Shared.Dtos.Courses;

public class CourseDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<LectureDto> Lectures { get; set; } = [];
    public List<Guid> TaskItemIds { get; set; } = [];
}