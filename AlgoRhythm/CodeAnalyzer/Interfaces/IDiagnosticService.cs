using AlgoRhythm.Shared.Dtos.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace CodeAnalyzer.Interfaces
{
    public interface IDiagnosticService
    {
        Task<DiagnosticDto[]> AnalyzeAsync(string code, string connectionId);
    }

}
