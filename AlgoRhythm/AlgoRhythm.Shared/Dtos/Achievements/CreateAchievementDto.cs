namespace AlgoRhythm.Shared.Dtos.Achievements;

public class CreateAchievementDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? IconPath { get; set; }
    public List<CreateRequirementDto> Requirements { get; set; } = new();
}

public class CreateRequirementDto
{
    public string? Description { get; set; }
    public required string Type { get; set; }
    public int TargetValue { get; set; }
    public Guid? TargetId { get; set; }
}