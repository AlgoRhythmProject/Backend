using AlgoRhythm.Shared.Dtos.CodeAnalysis;
using CodeAnalyzer.Interfaces;
using CodeAnalyzer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

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
        /// Maps completion item kinds to their priority for sorting.
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
        /// Retrieves completion items
        /// </summary>
        /// <param name="code">Source code of user</param>
        /// <param name="line">Line number (0-indexed).</param>
        /// <param name="column">Column number (0-indexed).</param>
        /// <param name="connectionId">Id of user's connection.</param>
        public async Task<CompletionItemDto[]> GetCompletionsAsync(string code, int line, int column, string connectionId)
        {
            _logger.LogInformation($"[GetCompletions] line={line}, column={column}");

            var session = _sessionManager.GetOrCreate(connectionId);
            var document = await _documentService.UpdateDocumentAsync(session, code);

            var position = await CalculatePosition(document, line, column);

            var (lastWord, isDotCompletion) = await GetCursorContext(document, position);

            var items = await GetCompletionItems(document, position, isDotCompletion);

            var itemsDto = ApplyFiltersAndSorting(items, lastWord, isDotCompletion);

            if (!isDotCompletion)
            {
                itemsDto = itemsDto.Concat(GetKeywordCompletions(lastWord));
            }

            return [.. itemsDto];
        }

        #region Helpers

        /// <summary>
        /// Calculates the absolute position in the Roslyn document for a given line and column.
        /// </summary>
        private static async Task<int> CalculatePosition(Document document, int line, int column)
        {
            var roslynLine = line + DocumentService.TemplateLineCount;
            var text = await document.GetTextAsync();

            if (roslynLine >= text.Lines.Count) return text.Length;

            return text.Lines[roslynLine].Start + column;
        }

        /// <summary>
        /// Returns the last typed word and whether the cursor is after a dot (member access).
        /// </summary>
        private static async Task<(string lastWord, bool isDotCompletion)> GetCursorContext(Document document, int position)
        {
            var text = await document.GetTextAsync();
            var textBeforeCursor = text.GetSubText(TextSpan.FromBounds(Math.Max(0, position - 30), position)).ToString();
            var isDot = textBeforeCursor.TrimEnd().EndsWith(".");

            var lineIndex = text.Lines.GetLineFromPosition(position).LineNumber;
            var lineStart = text.Lines[lineIndex].Start;
            var typedText = text.GetSubText(TextSpan.FromBounds(lineStart, position)).ToString();
            var lastWordMatch = Regex.Match(typedText.TrimEnd(), @"(\w+)$");
            var lastWord = lastWordMatch.Success ? lastWordMatch.Value : "";

            return (lastWord, isDot);
        }

        /// <summary>
        /// Gets completion items from Roslyn or falls back to manual completions.
        /// </summary>
        private async Task<IEnumerable<CompletionItem>> GetCompletionItems(Document document, int position, bool isDotCompletion)
        {
            var service = Microsoft.CodeAnalysis.Completion.CompletionService.GetService(document);
            if (service == null) return [];

            var result = await service.GetCompletionsAsync(document, position);
            return result?.ItemsList ?? [];
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


        private static IEnumerable<CompletionItemDto> ApplyFiltersAndSorting(IEnumerable<CompletionItem> items, string lastWord, bool isDot)
        {
            return items
                .Where(item => string.IsNullOrEmpty(lastWord) ||
                           item.DisplayText.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase)) 
                .Select(item => new { Item = item, Priority = GetPriority(item.Tags, isDot) })
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.Item.DisplayText)
                .Take(50) 
                .Select(x => MapToDto(x.Item, isDot));
        }

        private static CompletionItemDto[] GetKeywordCompletions(string filter)
        {
            var keywords = SyntaxFacts.GetKeywordKinds()
                .Concat(SyntaxFacts.GetContextualKeywordKinds())
                .Select(SyntaxFacts.GetText)
                .Where(k => !string.IsNullOrEmpty(k))
                .Distinct()
                .Where(k => string.IsNullOrEmpty(filter) || k.StartsWith(filter, StringComparison.OrdinalIgnoreCase));

            return [.. keywords.Select(k => new CompletionItemDto
            {
                Label = k!,
                Kind = (int)CompletionItemKind.Keyword,
                InsertText = k!,
                Detail = "keyword",
                SortText = $"9000_{k}"
            })];
        }

        /// <summary>
        /// Maps Roslyn SymbolKind to Monaco Editor CompletionItemKind.
        /// </summary>
        private static readonly Dictionary<string, CompletionItemKind> RoslynTagToMonacoKind = new(StringComparer.OrdinalIgnoreCase)
        {
            // Types
            { "Class", CompletionItemKind.Class },
            { "Structure", CompletionItemKind.Struct },
            { "Interface", CompletionItemKind.Interface },
            { "Enum", CompletionItemKind.Enum },
            { "Delegate", CompletionItemKind.Interface },
            { "Module", CompletionItemKind.Module },
    
            // Members
            { "Method", CompletionItemKind.Method },
            { "ExtensionMethod", CompletionItemKind.Method },
            { "Field", CompletionItemKind.Field },
            { "Property", CompletionItemKind.Property },
            { "Event", CompletionItemKind.Event },
            { "Constant", CompletionItemKind.Constant },
    
            // Vars and params
            { "Local", CompletionItemKind.Variable },
            { "Parameter", CompletionItemKind.Variable },
            { "RangeVariable", CompletionItemKind.Variable },
    
            // Others
            { "Keyword", CompletionItemKind.Keyword },
            { "Namespace", CompletionItemKind.Module },
            { "Label", CompletionItemKind.Text },
            { "Operator", CompletionItemKind.Operator }
        };

        #endregion
    }
}