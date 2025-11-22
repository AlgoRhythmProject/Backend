namespace AlgoRhythm.Shared.Dtos.Submissions;

public class SubmissionResponseDto
{
    public Guid SubmissionId { get; set; }
    public Guid TaskItemId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = null!;
    public double? Score { get; set; }
    public bool IsSolved { get; set; }
    public DateTime SubmittedAt { get; set; }
    public IReadOnlyList<TestResultDto> TestResults { get; set; } = [];
    public string? ErrorMessage { get; set; }
}
