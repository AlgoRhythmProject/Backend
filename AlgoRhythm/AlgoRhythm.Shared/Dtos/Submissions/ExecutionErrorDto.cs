using AlgoRhythm.Shared.Models.Submissions;
using Microsoft.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AlgoRhythm.Shared.Dtos.Submissions
{
    /// <summary>
    /// Represents detailed location information for a C# compilation or runtime error,
    /// including line and column positions within the source file.
    /// </summary>
    public class ExecutionErrorDto
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
        public ExecutionErrorDto(Diagnostic diagnostic)
        {
            ErrorMessage = diagnostic.GetMessage();

            FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
            
            StartLine = span.StartLinePosition.Line;     
            StartColumn = span.StartLinePosition.Character;
            EndLine = span.EndLinePosition.Line;
            EndColumn = span.EndLinePosition.Character;
            FilePath = span.Path;
        }

        /// <summary>
        /// Represents runtime error
        /// </summary>
        /// <param name="message"></param>
        public ExecutionErrorDto(string message)
        {
            ErrorMessage = message;
        }

        public ExecutionErrorDto(ExecutionError executionError)
        {
            ErrorMessage = executionError.ErrorMessage;
            StartLine = executionError.StartLine.GetValueOrDefault();
            EndLine = executionError.EndLine.GetValueOrDefault();
            StartColumn = executionError.StartColumn.GetValueOrDefault();
            EndColumn = executionError.EndColumn.GetValueOrDefault();
            FilePath = executionError.FilePath;
        }

        /// <summary>
        /// Empty constructor required for serialization
        /// </summary>
        [JsonConstructor]
        public ExecutionErrorDto() { }
    }
}
