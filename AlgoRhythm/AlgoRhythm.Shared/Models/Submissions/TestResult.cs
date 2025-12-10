using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AlgoRhythm.Shared.Models.Tasks;

namespace AlgoRhythm.Shared.Models.Submissions;

public class TestResult
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SubmissionId { get; set; }

    [Required]
    public Guid TestCaseId { get; set; }

    public bool Passed { get; set; } = false;

    public int Points { get; set; } = 0;

    public double ExecutionTimeMs { get; set; } = 0;

    public string? StdOut { get; set; }

    public string? StdErr { get; set; }

    [ForeignKey(nameof(SubmissionId))]
    public ProgrammingSubmission Submission { get; set; } = null!;
    
    [ForeignKey(nameof(TestCaseId))]
    public TestCase TestCase { get; set; } = null!;
}
