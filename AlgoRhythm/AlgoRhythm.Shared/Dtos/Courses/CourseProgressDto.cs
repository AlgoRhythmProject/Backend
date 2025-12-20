namespace AlgoRhythm.Shared.Dtos.Courses;

public class CourseProgressDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    public string? CourseName { get; set; }
    
    /// <summary>
    /// Automatic percentage based on completed lectures and tasks
    /// </summary>
    public int Percentage { get; set; }
    
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public int TotalLectures { get; set; }
    public int CompletedLecturesCount { get; set; }
    public List<Guid> CompletedLectureIds { get; set; } = new();
    
    public int TotalTasks { get; set; }
    public int CompletedTasksCount { get; set; }
    public List<Guid> CompletedTaskIds { get; set; } = new();
}