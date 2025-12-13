using AlgoRhythm.Shared.Models.CodeExecution.Requests;
using AlgoRhythm.Shared.Models.Tasks;

namespace AlgoRhythm.Services.Interfaces;

public interface ICodeParser
{
    public ExecuteCodeRequest ParseToExecuteRequest(string code, string? inputJson = null, TimeSpan? timeout = null);

    public List<ExecuteCodeRequest> BuildRequestsForTestCases(string code, IEnumerable<TestCase> testCases, TimeSpan? timeout = null);

    public void ValidateArguments(string code, IEnumerable<TestCase> testCases);
}
