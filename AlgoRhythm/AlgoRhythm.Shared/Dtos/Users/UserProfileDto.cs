namespace AlgoRhythm.Shared.Dtos.Users;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = [];
    
    // Statistics
    public int TotalSubmissions { get; set; }
    public int SolvedTasks { get; set; }
    public int TotalPoints { get; set; }
    public int AchievementsCount { get; set; }
    public int CoursesInProgress { get; set; }
    public int CompletedCourses { get; set; }
    
    // Preferences
    public UserPreferencesDto? Preferences { get; set; }
}