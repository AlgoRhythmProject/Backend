using CodeAnalyzer.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CodeAnalyzer.Services
{
    public class DocumentService : IDocumentService
    {
        private static string TemplateCode => @"
             using System;
             using System.Collections.Generic;
             using System.Linq;
             using System.Text;
             using System.Threading.Tasks;

             {0}

             public class Program
             {{
                 public static void Main() {{ }}
             }}";


        public static int TemplateLineCount => TemplateCode
                                                    .Split(Environment.NewLine)
                                                    .TakeWhile(line => !line.Contains("{0}"))
                                                    .Count();

        public Document UpdateDocument(SessionState session, string userCode)
        {
            var fullCode = WrapCode(userCode);
            var solution = session.Workspace.CurrentSolution
                .WithDocumentText(session.DocumentId, SourceText.From(fullCode));

            session.Workspace.TryApplyChanges(solution);
            return session.Workspace.CurrentSolution.GetDocument(session.DocumentId)!;
        }

        public int ToRoslynPosition(Document document, int line, int column)
        {
            var text = document.GetTextAsync().Result;
            var roslynLine = line + TemplateLineCount;

            if (roslynLine >= text.Lines.Count)
                throw new ArgumentOutOfRangeException(nameof(line));

            return text.Lines[roslynLine].Start + column;
        }

        private static string WrapCode(string userCode) => string.Format(TemplateCode, userCode);
    }

}
