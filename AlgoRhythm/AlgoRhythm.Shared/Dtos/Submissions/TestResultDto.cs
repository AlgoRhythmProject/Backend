namespace AlgoRhythm.Api.Dtos;

public class TestResultDto
{
    public Guid TestCaseId { get; set; }
    public bool Passed { get; set; }
    public int Points { get; set; }
    public double ExecutionTimeMs { get; set; }
    public string? StdOut { get; set; }
    public string? StdErr { get; set; }
}
