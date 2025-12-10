using AlgoRhythm.Shared.Models.CodeExecution;
using AlgoRhythm.Shared.Models.Submissions;

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

public static class TestResultExtensions
{
    public static SubmissionStatus ToSubmissionStatus(this IEnumerable<TestResultDto> results)
    {
        return results switch
        {
            _ when results.All(r => r.Passed) => SubmissionStatus.Accepted,
            _ when results.All(r => string.IsNullOrEmpty(r.StdErr) && r.Errors.Count == 0) => SubmissionStatus.Rejected,
            _ => SubmissionStatus.Error,
        };
    }
}
