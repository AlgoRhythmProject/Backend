namespace AlgoRhythm.Shared.Dtos.Users;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime? LastLoginDate { get; set; }
}