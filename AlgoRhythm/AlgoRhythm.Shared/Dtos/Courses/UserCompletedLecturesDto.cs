namespace AlgoRhythm.Shared.Dtos.Courses;

/// <summary>
/// DTO containing all completed lectures for a user across all courses.
/// </summary>
public class UserCompletedLecturesDto
{
    public Guid UserId { get; set; }
    public List<Guid> CompletedLectureIds { get; set; } = new();
    public int TotalCompleted { get; set; }
}