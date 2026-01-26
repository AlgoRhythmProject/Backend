using AlgoRhythm.Shared.Dtos.CodeAnalysis;

namespace CodeAnalyzer.Interfaces
{
    public interface ICompletionService
    {
        Task<CompletionItemDto[]> GetCompletionsAsync(string code, int line, int column, string connectionId);
    }
}
