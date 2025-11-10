using System.ComponentModel.DataAnnotations;

namespace AlgoRhythm.Models.Achievements;

public class Achievement
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? IconPath { get; set; }

    public ICollection<Requirement> Requirements { get; set; } = new List<Requirement>();
    public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}