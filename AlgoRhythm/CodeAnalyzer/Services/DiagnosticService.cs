using AlgoRhythm.Shared.Dtos.CodeAnalysis;
using CodeAnalyzer.Interfaces;
using Microsoft.CodeAnalysis;

namespace CodeAnalyzer.Services
{
    public class DiagnosticService : IDiagnosticService
    {
        private readonly IDocumentService _documentService;
        private readonly ISessionManager _sessionManager;

        public DiagnosticService(ISessionManager sessionManager, IDocumentService documentService)
        {
            _documentService = documentService;
            _sessionManager = sessionManager;
        }

        public async Task<DiagnosticDto[]> AnalyzeAsync(string code, string connectionId)
        {
            var session = _sessionManager.GetOrCreate(connectionId);
            var document = _documentService.UpdateDocument(session, code);

            var compilation = await document.Project.GetCompilationAsync();
            if (compilation == null) return [];

            var diagnostics = compilation.GetDiagnostics();

            return diagnostics
                .Where(d => d.Location.IsInSource)
                .Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning)
                .Select(d => {
                    var mappedLineSpan = d.Location.GetLineSpan();
                    var startLine = mappedLineSpan.StartLinePosition.Line - DocumentService.TemplateLineCount;
                    var endLine = mappedLineSpan.EndLinePosition.Line - DocumentService.TemplateLineCount;

                    if (startLine < 0) return null;

                    return new DiagnosticDto
                    {
                        Message = d.GetMessage(),
                        Severity = (int)d.Severity,
                        StartLine = startLine,
                        StartColumn = mappedLineSpan.StartLinePosition.Character,
                        EndLine = endLine,
                        EndColumn = mappedLineSpan.EndLinePosition.Character,
                        Id = d.Id
                    };
                })
                .Where(d => d != null)
                .ToArray()!;
        }
    }
}
