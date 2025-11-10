using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AlgoRhythm.Models.Submissions;

namespace AlgoRhythm.Models.Tasks;

public class TestCase
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProgrammingTaskItemId { get; set; }

    public string? InputJson { get; set; }

    public string? ExpectedJson { get; set; }

    public bool IsVisible { get; set; } = true;

    public int MaxPoints { get; set; } = 10;

    [ForeignKey(nameof(ProgrammingTaskItemId))]
    public ProgrammingTaskItem ProgrammingTaskItem { get; set; } = null!;
    
    public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}