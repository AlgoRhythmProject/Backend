using AlgoRhythm.Shared.Models.Submissions;

namespace AlgoRhythm.Shared.Dtos.Submissions;

public class SubmissionHistoryDto
{
    public Guid Id { get; set; }
    public Guid TaskItemId { get; set; }
    public string? TaskTitle { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
    public double? Score { get; set; }
    public DateTime SubmittedAt { get; set; }
    public bool IsSolved { get; set; }
}