using Microsoft.CodeAnalysis;

namespace CodeAnalyzer.Interfaces
{
    public interface IWorkspaceFactory
    {
        (AdhocWorkspace workspace, ProjectId projectId, DocumentId documentId) Create();
    }

}
