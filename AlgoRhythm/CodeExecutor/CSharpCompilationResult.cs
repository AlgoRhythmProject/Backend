using Microsoft.CodeAnalysis;

namespace CodeExecutor
{
    public class CSharpCompilationResult
    {
        public bool Success { get; set; }
        public MemoryStream? AssemblyStream { get; set; }
        public List<string> Errors { get; set; } = new();
        public Dictionary<string, ITypeSymbol> ParsedArgs { get; set; } = new();

        public CSharpCompilationResult(bool success, MemoryStream? assemblyStream, List<string> errors, Dictionary<string, ITypeSymbol> parsedArgs)
        {
            Success = success;
            AssemblyStream = assemblyStream;
            Errors = errors;
            ParsedArgs = parsedArgs;
        }

        // In case of compilation errors, we don't parse the function arguments
        public CSharpCompilationResult(bool success, MemoryStream? assemblyStream, List<string> errors)
        {
            Success = success; 
            AssemblyStream = assemblyStream; 
            Errors = errors;
        }
    }
}
