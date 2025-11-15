using CodeExecutor.DTO;

namespace CodeExecutor.Interfaces
{
    public interface ICodeExecutionService
    {
        Task<ExecutionResult> ExecuteCodeAsync(string code);
    }
}
