using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace AlgoRhythm.Shared.Models.Achievements;

public class Requirement
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AchievementId { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// JSON serialized RequirementCondition
    /// </summary>
    public string? ConditionJson { get; set; }

    [ForeignKey(nameof(AchievementId))]
    public Achievement Achievement { get; set; } = null!;

    public ICollection<UserRequirementProgress> UserRequirementProgresses { get; set; } = new List<UserRequirementProgress>();

    [NotMapped]
    public RequirementCondition? Condition
    {
        get => string.IsNullOrEmpty(ConditionJson) 
            ? null 
            : JsonSerializer.Deserialize<RequirementCondition>(ConditionJson);
        set => ConditionJson = value == null 
            ? null 
            : JsonSerializer.Serialize(value);
    }
}

public class RequirementCondition
{
    public RequirementType Type { get; set; }
    public int TargetValue { get; set; }
    public Guid? TargetId { get; set; } // Optional: specific course/task/lecture ID
}

public enum RequirementType
{
    CompleteTasks,
    CompleteLectures,
    CompleteCourses,
    CompleteSpecificCourse,
    CompleteSpecificTask,
    CompleteSpecificLecture,
    LoginStreak
}