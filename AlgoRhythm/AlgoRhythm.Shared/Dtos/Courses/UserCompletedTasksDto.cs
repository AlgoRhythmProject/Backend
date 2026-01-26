namespace AlgoRhythm.Shared.Dtos.Courses;

/// <summary>
/// DTO containing all completed tasks for a user across all courses.
/// </summary>
public class UserCompletedTasksDto
{
    public Guid UserId { get; set; }
    public List<Guid> CompletedTaskIds { get; set; } = new();
    public int TotalCompleted { get; set; }
}