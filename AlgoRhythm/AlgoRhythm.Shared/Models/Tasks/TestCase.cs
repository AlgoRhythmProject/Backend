using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AlgoRhythm.Shared.Models.Submissions;

namespace AlgoRhythm.Shared.Models.Tasks;

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

    /// <summary>
    /// Optional custom timeout in milliseconds for this test case.
    /// If null, the default timeout (5000ms = 5 seconds) will be used.
    /// </summary>
    public int? TimeoutMs { get; set; }

    [ForeignKey(nameof(ProgrammingTaskItemId))]
    public ProgrammingTaskItem ProgrammingTaskItem { get; set; } = null!;
    
    public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}