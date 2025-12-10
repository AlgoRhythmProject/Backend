namespace AlgoRhythm.Shared.Dtos.Achievements;

public class AchievementInputDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? IconPath { get; set; }
}