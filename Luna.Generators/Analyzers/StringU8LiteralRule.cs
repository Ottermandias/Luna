using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#pragma warning disable RS2008

namespace Luna.Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StringU8LiteralRule : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(AnalyzeMethod, OperationKind.ObjectCreation);
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Rule];

    private static void AnalyzeMethod(OperationAnalysisContext context)
    {
        var syntax = (IObjectCreationOperation)context.Operation;
        if (syntax.Arguments.Length is not 1)
            return;

        // Unwrap any conversions taking place, specifically from the string literal to RoS<char>.
        var v = Unwrap(syntax.Arguments[0].Value);
        if (!v.ConstantValue.HasValue || v.ConstantValue.Value is not string)
        {
            // If we do not recognize a constant value, at least check against a string literal.
            if (v.Syntax is not LiteralExpressionSyntax literal || !literal.IsKind(SyntaxKind.StringLiteralExpression))
                return;
        }

        var targetType = context.Compilation.GetTypeByMetadataName("ImSharp.StringU8");
        if (syntax.Type is not {} type || !SymbolEqualityComparer.Default.Equals(type, targetType))
            return;

        var diagnostic = Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }

    private static readonly DiagnosticDescriptor Rule = new("Luna04", "Prefer UTF8 Literals",
        "You are supplying a UTF16 literal to a StringU8 constructor, prefer to supply a UTF8 literal",
        "Optimization", DiagnosticSeverity.Warning, true);

    private static IOperation Unwrap(IOperation op)
    {
        while (op is IConversionOperation conv)
            op = conv.Operand;
        return op;
    }
}
