using AlgoRhythm.Shared.Dtos.CodeAnalysis;
using CodeAnalyzer.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.QuickInfo;
using System.Globalization;

namespace CodeAnalyzer.Services
{
    public class QuickInfoRoslynService : IQuickInfoService
    {
        private readonly ISessionManager _sessionManager;
        private readonly IDocumentService _documentService;
        private readonly ILogger<QuickInfoRoslynService> _logger;

        public QuickInfoRoslynService(ISessionManager sessionManager, IDocumentService documentService, ILogger<QuickInfoRoslynService> logger)
        {
            _sessionManager = sessionManager;
            _documentService = documentService;
            _logger = logger;
        }

        public async Task<QuickInfoDto?> GetQuickInfoAsync(string code, int line, int column, string connectionId)
        {
            var session = _sessionManager.GetOrCreate(connectionId);
            var document = await _documentService.UpdateDocumentAsync(session, code);

            var roslynLine = line + DocumentService.TemplateLineCount;
            var text = await document.GetTextAsync();
            if (roslynLine >= text.Lines.Count) return null;

            var position = text.Lines[roslynLine].Start + column;

            // QuickInfoService
            var service = QuickInfoService.GetService(document);
            if (service != null)
            {
                return await GetFromRoslyn(document, position, service);
            }

            // Manual QuickInfo
            return await GetManualCompletions(document, position);
        }

        private static async Task<QuickInfoDto?> GetFromRoslyn(Document document, int position, QuickInfoService service)
        {
            var info = await service.GetQuickInfoAsync(document, position);
            if (info != null)
            {
                var descriptionSection = info.Sections.FirstOrDefault(s => s.Kind == QuickInfoSectionKinds.Description);
                var description = descriptionSection != null ? string.Concat(descriptionSection.TaggedParts.Select(p => p.Text)) : "";

                var docSection = info.Sections.FirstOrDefault(s => s.Kind == QuickInfoSectionKinds.DocumentationComments);
                var documentation = docSection != null ? string.Concat(docSection.TaggedParts.Select(p => p.Text)) : "";

                return new QuickInfoDto
                {
                    Description = description,
                    Documentation = documentation,
                    SpanStart = info.Span.Start,
                    SpanLength = info.Span.Length
                };
            }

            return null;
        }

        private static async Task<QuickInfoDto?> GetManualCompletions(Document document, int position)
        {
            var semanticModel = await document.GetSemanticModelAsync();
            var syntaxRoot = await document.GetSyntaxRootAsync();

            if (semanticModel == null || syntaxRoot == null) return null;

            var token = syntaxRoot.FindToken(position);
            var node = token.Parent;

            if (node == null) return null;

            var symbol = semanticModel.GetSymbolInfo(node).Symbol;

            if (symbol == null) return null;

            return new QuickInfoDto
            {
                Description = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                Documentation = symbol.GetDocumentationCommentXml(
                                    preferredCulture: CultureInfo.CurrentCulture,
                                    expandIncludes: true,
                                    cancellationToken: default
                                ) ?? "",
                SpanStart = token.Span.Start,
                SpanLength = token.Span.Length
            };
        }
    }
}
