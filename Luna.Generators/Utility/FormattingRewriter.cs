using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Luna.Generators;

internal sealed class FormattingRewriter(MethodDeclarationSyntax template) : CSharpSyntaxRewriter
{
    private readonly int _constraintIndent = template.ConstraintClauses.FirstOrDefault() is { } constraint
        ? constraint.GetLocation().GetLineSpan().StartLinePosition.Character
        : 0;

    private readonly int _expressionIndent = template.ExpressionBody is { } expression
        ? expression.GetLocation().GetLineSpan().StartLinePosition.Character
        : 0;

    private bool _firstMethod = true;

    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var result = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;

        if (_firstMethod)
        {
            _firstMethod = false;
            return result;
        }

        return result.WithLeadingTrivia(result.GetLeadingTrivia().Insert(0, SyntaxFactory.LineFeed));
    }

    public override SyntaxNode VisitTypeParameterConstraintClause(TypeParameterConstraintClauseSyntax node)
    {
        var result = (TypeParameterConstraintClauseSyntax)base.VisitTypeParameterConstraintClause(node)!;
        return result.WithLeadingTrivia(LineBreakIfNeeded(node, _constraintIndent));
    }

    public override SyntaxNode VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
    {
        var result = (ArrowExpressionClauseSyntax)base.VisitArrowExpressionClause(node)!;
        return result.WithLeadingTrivia(LineBreakIfNeeded(node, _expressionIndent));
    }

    private static SyntaxTriviaList LineBreakIfNeeded(SyntaxNode node, int indentation)
    {
        var previous = node.GetFirstToken().GetPreviousToken();

        var trivia = previous.TrailingTrivia.Any(static t => t.IsKind(SyntaxKind.EndOfLineTrivia))
            ? SyntaxFactory.TriviaList()
            : SyntaxFactory.TriviaList(SyntaxFactory.LineFeed);

        return trivia.Add(SyntaxFactory.Whitespace(new string(' ', indentation)));
    }
}
