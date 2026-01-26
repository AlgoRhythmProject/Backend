namespace AlgoRhythm.Shared.Dtos.Achievements;

public class EarnedAchievementDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? IconPath { get; set; }
}