namespace AlgoRhythm.Shared.Dtos.Achievements;

public class RequirementInputDto
{
    public Guid AchievementId { get; set; }
    public string? Description { get; set; }
    public string? ConditionJson { get; set; }
}