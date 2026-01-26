using CodeAnalyzer.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;

namespace CodeAnalyzer.Services
{
    public sealed class WorkspaceFactory : IWorkspaceFactory
    {
        private readonly HostServices _hostServices;
        private readonly IReferenceProvider _references;

        public WorkspaceFactory(
            HostServices hostServices,
            IReferenceProvider references)
        {
            _hostServices = hostServices;
            _references = references;
        }

        public (AdhocWorkspace, ProjectId, DocumentId) Create()
        {
            var workspace = new AdhocWorkspace(_hostServices);

            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                "ScriptProject",
                "ScriptAssembly",
                LanguageNames.CSharp,
                metadataReferences: _references.GetReferences(),
                parseOptions: new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse),
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            workspace.TryApplyChanges(
                workspace.CurrentSolution
                    .AddProject(projectInfo)
                    .AddDocument(documentId, "Program.cs", "")
            );

            return (workspace, projectId, documentId);
        }
    }

}
