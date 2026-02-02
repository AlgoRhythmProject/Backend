using AlgoRhythm.Shared.Models.Submissions;

namespace AlgoRhythm.Shared.Dtos.Submissions;

public class SubmissionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public Guid TaskItemId { get; set; }
    public string? TaskTitle { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
    public double? Score { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string Type { get; set; } = "submission";
}