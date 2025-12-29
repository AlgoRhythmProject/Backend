namespace AlgoRhythm.Shared.Dtos.Achievements;

public class RequirementProgressDto
{
    public Guid RequirementId { get; set; }
    public string? Description { get; set; }
    public int CurrentValue { get; set; }
    public int TargetValue { get; set; }
    public bool IsSatisfied { get; set; }
    public double ProgressPercentage { get; set; }
}