using CodeAnalyzer.Interfaces;
using CodeAnalyzer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CodeAnalyzer.Services
{
    /// <summary>
    /// Manages the lifecycle of Roslyn documents within a workspace.
    /// Handles code wrapping into templates, position mapping, and synchronization 
    /// between the user's raw input and the virtual compilation environment.
    /// </summary>
    public class DocumentService : IDocumentService
    {
        private static string TemplateCode => @"
             {0}

             public class Program
             {{
                 public static void Main() {{ }}
             }}";

        public static int TemplateLineCount => TemplateCode
                                                    .Split(Environment.NewLine)
                                                    .TakeWhile(line => !line.Contains("{0}"))
                                                    .Count();

        /// <summary>
        /// Updates the current document in the session's workspace with new content.
        /// </summary>
        /// <param name="session">The current user session containing the Workspace and Document IDs.</param>
        /// <param name="userCode">The raw code string from the Monaco editor.</param>
        /// <returns>The updated <see cref="Document"/> instance from the Roslyn workspace.</returns>
        public async Task<Document> UpdateDocumentAsync(SessionState session, string userCode)
        {
            var fullCode = WrapCode(userCode);
            var sourceText = SourceText.From(fullCode);

            var currentDocument = session.Workspace.CurrentSolution.GetDocument(session.DocumentId);

            if (currentDocument != null)
            {
                var oldText = await currentDocument.GetTextAsync();
                if (oldText.ToString() == fullCode)
                    return currentDocument;

                var newSolution = currentDocument.Project.Solution.WithDocumentText(session.DocumentId, sourceText);

                if (session.Workspace.TryApplyChanges(newSolution))
                {
                    return session.Workspace.CurrentSolution.GetDocument(session.DocumentId)!;
                }
            }

            return currentDocument ?? throw new Exception("Invalid session state!");
        }

        /// <summary>
        /// Maps a line and column from the Monaco Editor (user view) 
        /// to an absolute character position in the Roslyn Document (compiler view).
        /// </summary>
        public async Task<int> ToRoslynPosition(Document document, int line, int column)
        {
            var text = await document.GetTextAsync();
            var roslynLine = line + TemplateLineCount;

            if (roslynLine >= text.Lines.Count)
                throw new ArgumentOutOfRangeException(nameof(line));

            return text.Lines[roslynLine].Start + column;
        }

        private static string WrapCode(string userCode) => string.Format(TemplateCode, userCode);
    }

}
