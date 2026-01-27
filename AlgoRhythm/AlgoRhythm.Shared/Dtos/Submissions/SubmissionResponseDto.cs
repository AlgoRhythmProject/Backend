using AlgoRhythm.Shared.Models.Submissions;

namespace AlgoRhythm.Shared.Dtos.Submissions;

public class SubmissionResponseDto
{
    public Guid SubmissionId { get; set; }
    public Guid TaskItemId { get; set; }
    public Guid UserId { get; set; }
    public SubmissionStatus Status { get; set; }
    public double? Score { get; set; }
    public bool IsSolved { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string Code { get; set; } = null!;
    public IReadOnlyList<TestResultDto> TestResults { get; set; } = [];
    public string? ErrorMessage { get; set; }
}
