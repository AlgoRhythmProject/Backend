namespace AlgoRhythm.Shared.Dtos.Submissions;

public class ProgrammingSubmissionDto : SubmissionDto
{
    public required string Code { get; set; }
    public DateTime ExecuteStartedAt { get; set; }
    public DateTime? ExecuteFinishedAt { get; set; }
    public bool IsSolved { get; set; }
    public List<TestResultDto> TestResults { get; set; } = [];
}