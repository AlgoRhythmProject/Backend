namespace AlgoRhythm.Shared.Dtos.Courses;

public class LectureDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public required string Title { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<LectureContentDto> Contents { get; set; } = [];
    public List<Guid> TagIds { get; set; } = [];
}