using Microsoft.CodeAnalysis;

namespace CodeAnalyzer
{
    public record SessionState(
        AdhocWorkspace Workspace,
        ProjectId ProjectId,
        DocumentId DocumentId
    );
}
