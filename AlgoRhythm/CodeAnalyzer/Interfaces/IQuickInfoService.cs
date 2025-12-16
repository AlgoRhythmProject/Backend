using AlgoRhythm.Shared.Dtos.CodeAnalysis;

namespace CodeAnalyzer.Interfaces
{
    public interface IQuickInfoService
    {
        Task<QuickInfoDto?> GetQuickInfoAsync(string code, int line, int column, string connectionId);
    }
}
