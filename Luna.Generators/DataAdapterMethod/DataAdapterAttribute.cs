using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Luna.Generators;

internal static class AdapterMethodAttribute
{
    public static string MetadataName      = $"Luna.Generators.{nameof(AdapterMethodAttribute)}";
    private const string MethodIdMember    = "MethodId";
    private const string MethodIdParameter = "methodId";

    public static CompilationUnitSyntax CreateAttribute()
    {
        var @namespace = SyntaxFactory.LunaGeneratorsNamespace();
        var embedded   = SyntaxFactory.Embedded();
        var generated  = SyntaxFactory.Generated();
        var comment =
            "/// <summary> Mark a method to be exposed through <see cref=\"global::Dalamud.Plugin.Ipc.IIdDataShareAdapter\"/>. </summary>"
                .Comment();
        var usage = SyntaxFactory.AttributeUsage(AttributeTargets.Method, AttributeTargets.Property);

        var methodIdProperty = SyntaxFactory.CreateProperty(MethodIdMember,
            "The unique ID of the invocation this method shall be associated with. This can either be an integer or an integer-based enum value.",
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)));

        var disposeAfterFailure = SyntaxFactory.CreateProperty("DisposeOnFailure",
            "Whether to dispose the created object if it is not assignable to the target type.", SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)));

        var ctorParameters = SyntaxFactory.ParameterList(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(MethodIdParameter.Identifier())
                    .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)))));

        var ctor = SyntaxFactory.ConstructorDeclaration(nameof(AdapterMethodAttribute).Identifier())
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(ctorParameters)
            .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                MethodIdMember.IdentifierName(), MethodIdParameter.IdentifierName())))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        var attribute = SyntaxFactory.ClassDeclaration(nameof(AdapterMethodAttribute).Identifier())
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InternalKeyword)))
            .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(embedded)),
                SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(generated)),
                SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(usage)))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("global::System.Attribute")))
            .AddMembers(ctor, methodIdProperty, disposeAfterFailure)
            .WithLeadingTrivia(comment);

        return SyntaxFactory.CompilationUnit().AddMembers(@namespace.AddMembers(attribute)).Normalize();
    }
}
