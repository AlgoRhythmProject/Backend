namespace AlgoRhythm.Shared.Dtos.Achievements;

public class RequirementDto
{
    public Guid Id { get; set; }
    public Guid AchievementId { get; set; }
    public string? Description { get; set; }
    public string? ConditionJson { get; set; }
}