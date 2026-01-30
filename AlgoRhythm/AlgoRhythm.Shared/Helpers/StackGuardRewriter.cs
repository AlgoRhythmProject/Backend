using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AlgoRhythm.Shared.Helpers
{
    /// <summary>
    /// Syntax rewriter that injects stack overflow protection into user-provided code.
    /// It traverses the Syntax Tree and inserts safety checks at the beginning of every method 
    /// and local function to prevent terminal process crashes (e.g. during deep recursion)
    /// </summary>
    public class StackGuardRewriter : CSharpSyntaxRewriter
    {
        /// <summary>
        /// The statement injected into the beginning of methods. 
        /// It forces the .NET Runtime to throw a catchable <see cref="InsufficientExecutionStackException"/> 
        /// instead of a fatal <see cref="StackOverflowException"/>.
        /// </summary>
        private static readonly StatementSyntax GuardStatement =
            SyntaxFactory.ParseStatement("System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();\n");

        /// <summary>
        /// Visits method declarations and injects the guard statement.
        /// Handles both block-bodied methods and expression-bodied members (=>).
        /// </summary>
        /// <param name="node">The original method declaration node.</param>
        /// <returns>The modified method declaration with stack protection.</returns>
        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.Body != null)
            {
                var newBody = InjectGuard(node.Body);
                return node.WithBody(newBody);
            }

            if (node.ExpressionBody != null)
            {
                return ConvertToBlockBody(node, node.ExpressionBody, node.ReturnType);
            }

            return base.VisitMethodDeclaration(node);
        }

        /// <summary>
        /// Visits local function statements (functions defined inside other methods) 
        /// and injects the guard statement. This is critical as recursion often happens in local functions.
        /// </summary>
        /// <param name="node">The original local function node.</param>
        /// <returns>The modified local function with stack protection.</returns>
        public override SyntaxNode? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            if (node.Body != null)
            {
                var newBody = InjectGuard(node.Body);
                return node.WithBody(newBody);
            }
            return base.VisitLocalFunctionStatement(node);
        }

        /// <summary>
        /// Inserts the <see cref="GuardStatement"/> as the first instruction in a code block.
        /// </summary>
        private BlockSyntax InjectGuard(BlockSyntax body)
        {
            var newStatements = body.Statements.Insert(0, GuardStatement);
            return body.WithStatements(newStatements);
        }

        /// <summary>
        /// Converts an expression-bodied member (=>) into a block-bodied member ({ ... })
        /// to allow the injection of the guard statement.
        /// </summary>
        private SyntaxNode ConvertToBlockBody(SyntaxNode node, ArrowExpressionClauseSyntax arrow, TypeSyntax returnType)
        {
            var isVoid = returnType is PredefinedTypeSyntax pts && pts.Keyword.IsKind(SyntaxKind.VoidKeyword);

            var statement = isVoid
                ? SyntaxFactory.ExpressionStatement(arrow.Expression)
                : (StatementSyntax)SyntaxFactory.ReturnStatement(arrow.Expression);

            var block = SyntaxFactory.Block(GuardStatement, statement);

            return node switch
            {
                MethodDeclarationSyntax m => m.WithExpressionBody(null).WithSemicolonToken(default).WithBody(block),
                LocalFunctionStatementSyntax l => l.WithExpressionBody(null).WithSemicolonToken(default).WithBody(block),
                _ => node
            };
        }
    }
}
