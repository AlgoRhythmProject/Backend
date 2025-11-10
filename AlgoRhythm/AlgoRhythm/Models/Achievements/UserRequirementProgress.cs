using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlgoRhythm.Models.Achievements;

public class UserRequirementProgress
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserAchievementId { get; set; }

    [Required]
    public Guid RequirementId { get; set; }

    public int ProgressValue { get; set; } = 0;

    public bool IsSatisfied { get; set; } = false;

    [ForeignKey(nameof(UserAchievementId))]
    public UserAchievement UserAchievement { get; set; } = null!;
    
    [ForeignKey(nameof(RequirementId))]
    public Requirement Requirement { get; set; } = null!;
}