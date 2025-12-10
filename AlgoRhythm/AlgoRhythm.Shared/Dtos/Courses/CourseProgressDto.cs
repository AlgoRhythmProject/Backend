namespace AlgoRhythm.Shared.Dtos.Courses;

public class CourseProgressDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    public string? CourseName { get; set; }
    public int Percentage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}