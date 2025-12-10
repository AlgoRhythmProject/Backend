namespace AlgoRhythm.Shared.Dtos.Achievements;

public class UserRequirementProgressDto
{
    public Guid Id { get; set; }
    public Guid RequirementId { get; set; }
    public string? RequirementDescription { get; set; }
    public int ProgressValue { get; set; }
    public bool IsSatisfied { get; set; }
}