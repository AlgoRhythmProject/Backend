using AlgoRhythm.Shared.Models.CodeExecution;

namespace AlgoRhythm.Shared.Dtos.Submissions;

public class TestResultDto
{
    public Guid TestCaseId { get; set; }
    public bool Passed { get; set; }
    public int Points { get; set; }
    public double ExecutionTimeMs { get; set; }
    public string? StdOut { get; set; }
    public string? StdErr { get; set; }
    public List<CSharpExecutionError> Errors { get; set; } = [];
    public long ExitCode { get; set; }
    public object? ReturnedValue { get; set; }
}
