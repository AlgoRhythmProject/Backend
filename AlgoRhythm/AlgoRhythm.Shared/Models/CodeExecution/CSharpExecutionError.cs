using Microsoft.CodeAnalysis;

namespace AlgoRhythm.Shared.Models.CodeExecution
{
    /// <summary>
    /// Represents detailed location information for a C# compilation or runtime error,
    /// including line and column positions within the source file.
    /// </summary>
    public class CSharpExecutionError
    {
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Represents a compilation error
        /// </summary>
        /// <param name="diagnostic"></param>
        public CSharpExecutionError(Diagnostic diagnostic)
        {
            ErrorMessage = diagnostic.GetMessage();

            FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
            
            // 0-based -> 1-based
            StartLine = span.StartLinePosition.Line + 1;     
            StartColumn = span.StartLinePosition.Character + 1;
            EndLine = span.EndLinePosition.Line + 1;
            EndColumn = span.EndLinePosition.Character + 1;
            FilePath = span.Path;
        }

        /// <summary>
        /// Represents runtime error
        /// </summary>
        /// <param name="message"></param>
        public CSharpExecutionError(string message)
        {  ErrorMessage = message; }
    }
}
