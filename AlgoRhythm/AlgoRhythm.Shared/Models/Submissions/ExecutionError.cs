using AlgoRhythm.Shared.Dtos.Submissions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlgoRhythm.Shared.Models.Submissions
{
    public class ExecutionError
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid TestResultId { get; set; }


        public int? StartLine { get; set; } = int.MinValue;
        public int? StartColumn { get; set; } = int.MinValue;
        public int? EndLine { get; set; } = int.MinValue;
        public int? EndColumn {  get; set; } = int.MinValue;

        public string FilePath { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        
        [ForeignKey(nameof(TestResultId))]
        public TestResult TestResult { get; set; } = null!;

        [NotMapped]
        public bool IsCompilationError => StartLine.HasValue;

    }

    public static class ExecutionErrorExtensions
    {
        public static IEnumerable<ExecutionError> FromDto(
            this IEnumerable<ExecutionErrorDto> executionErrorDtos)
        {
            return [.. executionErrorDtos.Select(dto => new ExecutionError()
            {                
                StartLine = dto.StartLine,
                EndLine = dto.EndLine,
                StartColumn = dto.StartColumn,
                EndColumn = dto.EndColumn,

                FilePath = dto.FilePath,
                ErrorMessage = dto.ErrorMessage
            })];
        }

        public static IEnumerable<ExecutionErrorDto> ToDto(
            this IEnumerable<ExecutionError> executionErrors)
        {
            return [.. executionErrors.Select(error => new ExecutionErrorDto(error))];
        }
    }
}
