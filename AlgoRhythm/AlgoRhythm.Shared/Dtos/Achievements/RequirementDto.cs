namespace AlgoRhythm.Shared.Dtos.Achievements;

public class RequirementDto
{
    public Guid Id { get; set; }
    public string? Description { get; set; }
    public string Type { get; set; } = null!;
    public int TargetValue { get; set; }
    public Guid? TargetId { get; set; }
}