using AlgoRhythm.Shared.Dtos.CodeAnalysis;
using CodeAnalyzer.Interfaces;
using Microsoft.CodeAnalysis;

namespace CodeAnalyzer.Services
{
    /// <summary>
    /// Provides real-time code analysis using the Roslyn compilation engine.
    /// Detects syntax errors, semantic issues, and warnings within the source code.
    /// </summary>
    public class DiagnosticService : IDiagnosticService
    {
        private readonly IDocumentService _documentService;
        private readonly ISessionManager _sessionManager;

        public DiagnosticService(ISessionManager sessionManager, IDocumentService documentService)
        {
            _documentService = documentService;
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Analyzes the provided source code and returns a collection of diagnostics.
        /// Filters results to include only relevant errors and warnings, mapping them back to the user's coordinate space.
        /// </summary>
        /// <param name="code">The raw C# source code provided by the user.</param>
        /// <param name="connectionId">The unique identifier for the user's active coding session.</param>
        /// <returns>An array of <see cref="DiagnosticDto"/> containing error/warning details and positions.</returns>
        public async Task<DiagnosticDto[]> AnalyzeAsync(string code, string connectionId)
        {
            var session = _sessionManager.GetOrCreate(connectionId);
            var document = await _documentService.UpdateDocumentAsync(session, code);

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
