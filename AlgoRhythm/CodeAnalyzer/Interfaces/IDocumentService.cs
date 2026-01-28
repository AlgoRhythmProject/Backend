using CodeAnalyzer.Models;
using Microsoft.CodeAnalysis;

namespace CodeAnalyzer.Interfaces
{
    public interface IDocumentService
    {
        Task<Document> UpdateDocumentAsync(SessionState session, string userCode);
        Task<int> ToRoslynPosition(Document document, int line, int column);
    }

}
