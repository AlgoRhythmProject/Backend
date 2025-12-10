namespace AlgoRhythm.Shared.Dtos.Courses;

public class LectureInputDto
{
    public Guid CourseId { get; set; }
    public required string Title { get; set; }
    public bool IsPublished { get; set; }
}