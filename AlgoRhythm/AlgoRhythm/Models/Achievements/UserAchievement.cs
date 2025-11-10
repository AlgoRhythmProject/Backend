using AlgoRhythm.Models.Users;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlgoRhythm.Models.Achievements;

public class UserAchievement
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid AchievementId { get; set; }

    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    public bool IsCompleted { get; set; } = false;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    
    [ForeignKey(nameof(AchievementId))]
    public Achievement Achievement { get; set; } = null!;
    
    public ICollection<UserRequirementProgress> RequirementProgresses { get; set; } = new List<UserRequirementProgress>();
}