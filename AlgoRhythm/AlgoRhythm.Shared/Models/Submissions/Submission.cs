using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AlgoRhythm.Shared.Models.Tasks;
using AlgoRhythm.Shared.Models.Users;

namespace AlgoRhythm.Shared.Models.Submissions;

public enum SubmissionStatus
{
    Pending,
    Accepted,
    Rejected,
    Error
}

public abstract class Submission
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid TaskItemId { get; set; }

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

    public double? Score { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
    
    [ForeignKey(nameof(TaskItemId))]
    public TaskItem TaskItem { get; set; } = null!;
}

public class ProgrammingSubmission : Submission
{
    [Required]
    public string Code { get; set; } = null!;

    public DateTime ExecuteStartedAt { get; set; }

    public DateTime? ExecuteFinishedAt { get; set; }

    public bool IsSolved { get; set; } = false;

    public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}