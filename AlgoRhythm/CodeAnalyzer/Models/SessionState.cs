using Microsoft.CodeAnalysis;

namespace CodeAnalyzer.Models
{
    public record SessionState(
        AdhocWorkspace Workspace,
        ProjectId ProjectId,
        DocumentId DocumentId
    );
}
