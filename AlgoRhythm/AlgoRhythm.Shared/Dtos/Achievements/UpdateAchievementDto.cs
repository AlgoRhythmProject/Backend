namespace AlgoRhythm.Shared.Dtos.Achievements;

public class UpdateAchievementDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? IconPath { get; set; }
    public List<UpdateRequirementDto>? Requirements { get; set; }
}

public class UpdateRequirementDto
{
    public Guid? Id { get; set; } // If null, create new; if provided, update existing
    public string? Description { get; set; }
    public string? Type { get; set; }
    public int? TargetValue { get; set; }
    public Guid? TargetId { get; set; }
    public bool ShouldDelete { get; set; } = false; // Mark for deletion
}