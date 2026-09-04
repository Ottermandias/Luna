using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Luna.Generators;

internal static class GenerateAritiesAttribute
{
    public const  string MetadataName   = $"Luna.Generators.{nameof(GenerateAritiesAttribute)}";
    private const string ArityMember    = "MaximumArity";
    private const string ArityParameter = "maximumArity";

    public static CompilationUnitSyntax CreateAttribute()
    {
        var @namespace = SyntaxFactory.LunaGeneratorsNamespace();
        var embedded   = SyntaxFactory.Embedded();
        var generated  = SyntaxFactory.Generated();
        var comment =
            "/// <summary> Mark a generic method to be duplicated up to <paramref cref=\"maximumArity\"/> times. </summary>"
                .Comment();
        var usage = SyntaxFactory.AttributeUsage(AttributeTargets.Method, AttributeTargets.Class, AttributeTargets.Struct);

        var arityParameter = SyntaxFactory.CreateProperty(ArityMember,
            "The maximum arity up to which copies of this method are generated.",
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)));

        var ctorParameters = SyntaxFactory.ParameterList(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(ArityParameter.Identifier())
                    .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))));

        var includeZeroArity = SyntaxFactory.CreateProperty("IncludeZeroArity",
            "Whether to create a zero-arity version of the function, too.",
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)));

        var ctor = SyntaxFactory.ConstructorDeclaration(nameof(GenerateAritiesAttribute).Identifier())
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(ctorParameters)
            .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                ArityMember.IdentifierName(), ArityParameter.IdentifierName())))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        var attribute = SyntaxFactory.ClassDeclaration(nameof(GenerateAritiesAttribute).Identifier())
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InternalKeyword)))
            .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(embedded)),
                SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(generated)),
                SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(usage)))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("global::System.Attribute")))
            .AddMembers(ctor, arityParameter, includeZeroArity)
            .WithLeadingTrivia(comment);

        return SyntaxFactory.CompilationUnit().AddMembers(@namespace.AddMembers(attribute)).Normalize();
    }
}
