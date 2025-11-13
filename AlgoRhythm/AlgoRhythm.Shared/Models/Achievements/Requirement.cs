using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlgoRhythm.Shared.Models.Achievements;

public class Requirement
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AchievementId { get; set; }

    public string? Description { get; set; }

    public string? ConditionJson { get; set; }

    [ForeignKey(nameof(AchievementId))]
    public Achievement Achievement { get; set; } = null!;
    
    public ICollection<UserRequirementProgress> UserRequirementProgresses { get; set; } = new List<UserRequirementProgress>();
}