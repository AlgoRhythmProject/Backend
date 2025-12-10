namespace AlgoRhythm.Shared.Dtos.Achievements;

public class UserAchievementDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AchievementId { get; set; }
    public string? AchievementName { get; set; }
    public string? AchievementDescription { get; set; }
    public string? IconPath { get; set; }
    public DateTime EarnedAt { get; set; }
    public bool IsCompleted { get; set; }
    public int CompletedRequirements { get; set; }
    public int TotalRequirements { get; set; }
    public List<UserRequirementProgressDto> RequirementProgresses { get; set; } = [];
}