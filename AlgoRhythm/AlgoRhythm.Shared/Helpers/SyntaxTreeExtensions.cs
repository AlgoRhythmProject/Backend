using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgoRhythm.Shared.Helpers
{
    public static class SyntaxTreeExtensions
    {
        public static SyntaxTree ToStackSafeSyntaxTree(this SyntaxTree tree)
        {
            StackGuardRewriter stackGuardRewriter = new();
            SyntaxNode root = tree.GetRoot();
            var modifiedRoot = stackGuardRewriter.Visit(root);
            return CSharpSyntaxTree.Create((CSharpSyntaxNode)modifiedRoot);
        }
    }
}
