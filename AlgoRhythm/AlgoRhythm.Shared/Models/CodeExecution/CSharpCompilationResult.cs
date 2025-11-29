
using AlgoRhythm.Shared.Models.CodeExecution;

namespace CodeExecutor
{
    /// <summary>
    /// Represents the result of a C# compilation, including success status,
    /// the generated assembly stream, and any compilation errors.
    /// </summary>
    public class CSharpCompilationResult
    {
        public bool Success { get; set; }
        public MemoryStream? AssemblyStream { get; set; }
        public List<CSharpExecutionError> Errors { get; set; } = new();


        public CSharpCompilationResult(bool success, MemoryStream? assemblyStream, List<CSharpExecutionError> errors)
        {
            Success = success; 
            AssemblyStream = assemblyStream; 
            Errors = errors;
        }
    }
}
