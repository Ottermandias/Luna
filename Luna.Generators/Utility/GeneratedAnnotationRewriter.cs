using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Luna.Generators;

internal sealed class GeneratedAnnotationRewriter : CSharpSyntaxRewriter
{
    private static readonly AttributeListSyntax GeneratedAttribute =
        SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Generated()));

    public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        => AddGeneratedAttribute((ClassDeclarationSyntax)base.VisitClassDeclaration(node)!);

    public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        => AddGeneratedAttribute((StructDeclarationSyntax)base.VisitStructDeclaration(node)!);

    public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        => AddGeneratedAttribute((ConstructorDeclarationSyntax)base.VisitConstructorDeclaration(node)!);

    public override SyntaxNode VisitDestructorDeclaration(DestructorDeclarationSyntax node)
        => AddGeneratedAttribute((DestructorDeclarationSyntax)base.VisitDestructorDeclaration(node)!);

    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        => AddGeneratedAttribute((MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!);

    public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        => AddGeneratedAttribute((PropertyDeclarationSyntax)base.VisitPropertyDeclaration(node)!);

    public override SyntaxNode VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        => AddGeneratedAttribute((IndexerDeclarationSyntax)base.VisitIndexerDeclaration(node)!);

    public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax node)
        => AddGeneratedAttribute((EventDeclarationSyntax)base.VisitEventDeclaration(node)!);

    public override SyntaxNode VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        => AddGeneratedAttribute((EventFieldDeclarationSyntax)base.VisitEventFieldDeclaration(node)!);

    public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        => AddGeneratedAttribute((FieldDeclarationSyntax)base.VisitFieldDeclaration(node)!);

    public override SyntaxNode VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        => AddGeneratedAttribute((OperatorDeclarationSyntax)base.VisitOperatorDeclaration(node)!);

    public override SyntaxNode VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
        => AddGeneratedAttribute((ConversionOperatorDeclarationSyntax)base.VisitConversionOperatorDeclaration(node)!);

    private static TypeDeclarationSyntax AddGeneratedAttribute(TypeDeclarationSyntax node)
    {
        var leading = node.GetLeadingTrivia();
        node = node.WithLeadingTrivia(default(SyntaxTriviaList));
        return node.WithAttributeLists(node.AttributeLists.Insert(0, GeneratedAttribute.WithLeadingTrivia(leading)));
    }

    private static MemberDeclarationSyntax AddGeneratedAttribute(MemberDeclarationSyntax node)
    {
        var leading = node.GetLeadingTrivia();
        node = node.WithLeadingTrivia(default(SyntaxTriviaList));
        return node.WithAttributeLists(node.AttributeLists.Insert(0, GeneratedAttribute.WithLeadingTrivia(leading)));
    }
}
