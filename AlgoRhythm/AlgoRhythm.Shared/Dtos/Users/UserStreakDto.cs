namespace AlgoRhythm.Shared.Dtos.Users;

public class UserStreakDto
{
    public Guid UserId { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime? LastLoginDate { get; set; }
}