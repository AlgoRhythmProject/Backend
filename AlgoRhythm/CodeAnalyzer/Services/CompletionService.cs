using AlgoRhythm.Shared.Dtos.CodeAnalysis;
using AlgoRhythm.Shared.Models.Common;
using CodeAnalyzer.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace CodeAnalyzer.Services
{
    /// <summary>
    /// Provides code completion items for C# using Roslyn, compatible with Monaco Editor.
    /// Handles filtering, sorting, and keyword suggestions.
    /// </summary>
    public class CompletionService : ICompletionService
    {
        private readonly ISessionManager _sessionManager;
        private readonly IDocumentService _documentService;
        private readonly ILogger<CompletionService> _logger;

        /// <summary>
        /// Maps completion item kinds to their priority for sorting in IntelliSense.
        /// Higher values appear first.
        /// </summary>
        private static readonly Dictionary<CompletionItemKind, int> KindPriority = new()
        {
            [CompletionItemKind.Method] = 100,
            [CompletionItemKind.Function] = 95,
            [CompletionItemKind.Constructor] = 90,
            [CompletionItemKind.Property] = 90,
            [CompletionItemKind.Field] = 60,
            [CompletionItemKind.Variable] = 85,     
            [CompletionItemKind.Class] = 75,
            [CompletionItemKind.Struct] = 70,
            [CompletionItemKind.Interface] = 70,
            [CompletionItemKind.Enum] = 65,
            [CompletionItemKind.Module] = 40,       
            [CompletionItemKind.Keyword] = 95
        };

        /// <summary>
        /// Maps completion item kinds to human-readable descriptions.
        /// Used for tooltips and QuickInfo.
        /// </summary>
        private static readonly Dictionary<CompletionItemKind, string> KindDescriptions = new()
        {
            [CompletionItemKind.Method] = "(method)",
            [CompletionItemKind.Function] = "(function)",
            [CompletionItemKind.Constructor] = "(constructor)",
            [CompletionItemKind.Property] = "(property)",
            [CompletionItemKind.Field] = "(field)",
            [CompletionItemKind.Variable] = "(variable)",  
            [CompletionItemKind.Class] = "(class)",
            [CompletionItemKind.Struct] = "(struct)",
            [CompletionItemKind.Interface] = "(interface)",
            [CompletionItemKind.Enum] = "(enum)",
            [CompletionItemKind.Module] = "(namespace)",  
            [CompletionItemKind.Keyword] = "(keyword)",
            [CompletionItemKind.Event] = "(event)",
            [CompletionItemKind.TypeParameter] = "(type parameter)"
        };


        public CompletionService(
            ISessionManager sessionManager, 
            IDocumentService documentService, 
            ILogger<CompletionService> logger)
        {
            _sessionManager = sessionManager;
            _documentService = documentService;
            _logger = logger;
        }

        /// <summary>
        /// Gets completion items at the specified line and column in the user's code.
        /// </summary>
        public async Task<CompletionItemDto[]> GetCompletionsAsync(string code, int line, int column, string connectionId)
        {
            _logger.LogInformation($"[GetCompletions] line={line}, column={column}");

            var session = _sessionManager.GetOrCreate(connectionId);
            var document = _documentService.UpdateDocument(session, code);

            var position = CalculatePosition(document, line, column);

            var (lastWord, isDotCompletion) = GetCursorContext(document, position);

            var items = await GetCompletionItems(document, position, isDotCompletion);

            items = ApplyFiltersAndSorting(items, lastWord, isDotCompletion);

            if (!isDotCompletion)
            {
                items = items.Concat(GetKeywordCompletions(lastWord));
            }

            return [.. items];
        }

        #region Helpers

        /// <summary>
        /// Calculates the absolute position in the Roslyn document for a given line and column.
        /// </summary>
        private static int CalculatePosition(Document document, int line, int column)
        {
            var roslynLine = line + DocumentService.TemplateLineCount;
            var text = document.GetTextAsync().Result; 
            
            if (roslynLine >= text.Lines.Count) return text.Length; 
            
            return text.Lines[roslynLine].Start + column;
        }

        /// <summary>
        /// Returns the last typed word and whether the cursor is after a dot (member access).
        /// </summary>
        private static (string lastWord, bool isDotCompletion) GetCursorContext(Document document, int position)
        {
            var text = document.GetTextAsync().Result;
            var textBeforeCursor = text.GetSubText(TextSpan.FromBounds(Math.Max(0, position - 50), position)).ToString();
            var isDot = textBeforeCursor.TrimEnd().EndsWith(".");

            var lineIndex = text.Lines.GetLineFromPosition(position).LineNumber;
            var lineStart = text.Lines[lineIndex].Start;
            var typedText = text.GetSubText(TextSpan.FromBounds(lineStart, position)).ToString();
            var lastWordMatch = System.Text.RegularExpressions.Regex.Match(typedText.TrimEnd(), @"(\w+)$");
            var lastWord = lastWordMatch.Success ? lastWordMatch.Value : "";

            return (lastWord, isDot);
        }

        /// <summary>
        /// Gets completion items from Roslyn or falls back to manual completions.
        /// </summary>
        private async Task<IEnumerable<CompletionItemDto>> GetCompletionItems(Document document, int position, bool isDotCompletion)
        {
            var service = Microsoft.CodeAnalysis.Completion.CompletionService.GetService(document);

            if (service != null)
            {
                var result = await service.GetCompletionsAsync(document, position);
                if (result != null)
                    return result.ItemsList.Select(item => MapToDto(item, isDotCompletion));
            }

            return [];
            // fallback - manual completions
            //return await GetManualCompletions(document, position);
        }

        /// <summary>
        /// Maps a Roslyn CompletionItem to a CompletionItemDto with priority and description.
        /// </summary>
        private static CompletionItemDto MapToDto(CompletionItem item, bool isDotCompletion)
        {
            var kind = MapKind(item.Tags);
            var priority = GetPriority(item.Tags, isDotCompletion);
            var detail = item.InlineDescription ?? GetKindDescription(item.Tags);

            return new CompletionItemDto
            {
                Label = item.DisplayText,
                Kind = kind,
                Detail = detail,
                InsertText = item.Properties.TryGetValue("InsertionText", out var t) ? t : item.DisplayText,
                Documentation = "",
                SortText = $"{9999 - priority:D4}_{item.DisplayText}"
            };
        }

        /// <summary>
        /// Maps string tags from Roslyn to CompletionItemKind enum.
        /// </summary>
        private static int MapKind(ImmutableArray<string> tags)
        {
            foreach (var tag in tags)
            {
                if (RoslynTagToMonacoKind.TryGetValue(tag, out var kind))
                {
                    return (int)kind;
                }
            }

            return (int)CompletionItemKind.Text;
        }

        /// <summary>
        /// Determines the priority of a completion item based on its kind and context.
        /// </summary>
        private static int GetPriority(ImmutableArray<string> tags, bool isDotCompletion)
        {
            foreach (var tag in tags)
            {
                if (Enum.TryParse<CompletionItemKind>(tag, out var kind) && KindPriority.TryGetValue(kind, out var prio))
                    return isDotCompletion ? prio : prio - 10;
            }
            return 50;
        }

       
        private static string GetKindDescription(ImmutableArray<string> tags)
        {
            foreach (var tag in tags)
            {
                if (Enum.TryParse<CompletionItemKind>(tag, ignoreCase: true, out var kind) &&
                    KindDescriptions.TryGetValue(kind, out var desc))
                    return desc;
            }
            return string.Empty;
        }


        private IEnumerable<CompletionItemDto> ApplyFiltersAndSorting(IEnumerable<CompletionItemDto> items, string lastWord, bool isDot)
        {
            return items
                .Where(i => string.IsNullOrEmpty(lastWord) || i.Label.Contains(lastWord, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(i =>
                {
                    var prio = int.Parse(i.SortText.Substring(0, 4));
                    return prio;
                })
                .ThenBy(i => i.Label)
                .Take(100);
        }

        private static CompletionItemDto[] GetKeywordCompletions(string filter)
        {
            var keywords = SyntaxFacts.GetKeywordKinds()
                .Concat(SyntaxFacts.GetContextualKeywordKinds())
                .Select(SyntaxFacts.GetText)
                .Where(k => !string.IsNullOrEmpty(k))
                .Distinct()
                .Where(k => string.IsNullOrEmpty(filter) || k.StartsWith(filter, StringComparison.OrdinalIgnoreCase));

            return keywords.Select(k => new CompletionItemDto
            {
                Label = k!,
                Kind = (int)CompletionItemKind.Keyword,
                InsertText = k!,
                Detail = "keyword",
                SortText = $"9000_{k}"
            }).ToArray();
        }

        //private async Task<CompletionItemDto[]> GetManualCompletions(Document document, int position)
        //{
        //    var semanticModel = await document.GetSemanticModelAsync();
        //    var syntaxRoot = await document.GetSyntaxRootAsync();
        //    if (semanticModel == null || syntaxRoot == null) return Array.Empty<CompletionItemDto>();

        //    var token = syntaxRoot.FindToken(position);
        //    var items = new List<CompletionItemDto>();

        //    // Member access
        //    var tokenLeft = syntaxRoot.FindToken(Math.Max(0, position - 1));
        //    if (tokenLeft.IsKind(SyntaxKind.DotToken))
        //    {
        //        var memberAccess = tokenLeft.Parent as MemberAccessExpressionSyntax;
        //        if (memberAccess != null)
        //        {
        //            var type = semanticModel.GetTypeInfo(memberAccess.Expression).Type;
        //            if (type != null)
        //            {
        //                var members = semanticModel.LookupSymbols(position, container: type, includeReducedExtensionMethods: true);
        //                items.AddRange(members
        //                    .Where(s => !s.IsImplicitlyDeclared && s.IsDefinition)
        //                    .Select(s => new CompletionItemDto
        //                    {
        //                        Label = s.Name,
        //                        Kind = SymbolKindToCompletionKind(s.Kind),
        //                        Detail = s.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
        //                        InsertText = s.Name
        //                    }));
        //            }
        //        }
        //    }

        //    // Local symbols
        //    var symbols = semanticModel.LookupSymbols(position).Take(50);
        //    items.AddRange(symbols.Select(s => new CompletionItemDto
        //    {
        //        Label = s.Name,
        //        Kind = SymbolKindToCompletionKind(s.Kind),
        //        Detail = s.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
        //        InsertText = s.Name
        //    }));

        //    return items.ToArray();
        //}

        /// <summary>
        /// Maps Roslyn SymbolKind to Monaco Editor CompletionItemKind.
        /// </summary>
        private static readonly Dictionary<string, CompletionItemKind> RoslynTagToMonacoKind = new(StringComparer.OrdinalIgnoreCase)
        {
            // Typy
            { "Class", CompletionItemKind.Class },
            { "Structure", CompletionItemKind.Struct }, // Roslyn: "Structure" -> Monaco: Struct
            { "Interface", CompletionItemKind.Interface },
            { "Enum", CompletionItemKind.Enum },
            { "Delegate", CompletionItemKind.Interface }, // Delegate często pasuje ikoną do interfejsu lub klasy
            { "Module", CompletionItemKind.Module },
    
            // Członkowie
            { "Method", CompletionItemKind.Method },
            { "ExtensionMethod", CompletionItemKind.Method }, // Roslyn ma osobny tag dla extension methods
            { "Field", CompletionItemKind.Field },
            { "Property", CompletionItemKind.Property },
            { "Event", CompletionItemKind.Event },
            { "Constant", CompletionItemKind.Constant },
    
            // Zmienne i parametry
            { "Local", CompletionItemKind.Variable },
            { "Parameter", CompletionItemKind.Variable },
            { "RangeVariable", CompletionItemKind.Variable },
    
            // Inne
            { "Keyword", CompletionItemKind.Keyword },
            { "Namespace", CompletionItemKind.Module }, // W Monaco Namespace to zazwyczaj Module
            { "Label", CompletionItemKind.Text },
            { "Operator", CompletionItemKind.Operator }
        };

        #endregion
    }
}
