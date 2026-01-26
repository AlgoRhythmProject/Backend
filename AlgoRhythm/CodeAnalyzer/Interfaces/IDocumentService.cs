using Microsoft.CodeAnalysis;

namespace CodeAnalyzer.Interfaces
{
    public interface IDocumentService
    {
        Document UpdateDocument(SessionState session, string userCode);
        int ToRoslynPosition(Document document, int line, int column);
    }

}
