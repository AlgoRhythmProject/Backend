using System.ComponentModel.DataAnnotations;
using AlgoRhythm.Shared.Models.Common;
using AlgoRhythm.Shared.Models.Courses;
using AlgoRhythm.Shared.Models.Submissions;

namespace AlgoRhythm.Shared.Models.Tasks;

public enum Difficulty
{
    Easy,
    Medium,
    Hard
}

public abstract class TaskItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public Difficulty Difficulty { get; set; } = Difficulty.Medium;

    public bool IsPublished { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public ICollection<Hint> Hints { get; set; } = new List<Hint>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}

public class ProgrammingTaskItem : TaskItem
{
    public string? TemplateCode { get; set; }

    public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
}

public class InteractiveTaskItem : TaskItem
{
    public string? OptionsJson { get; set; }

    public string? CorrectAnswer { get; set; }
}