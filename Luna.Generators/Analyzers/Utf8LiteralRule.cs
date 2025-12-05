using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#pragma warning disable RS2008

namespace Luna.Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Utf8LiteralRule : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(AnalyzeMethod, OperationKind.Conversion);
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Rule];

    private static void AnalyzeMethod(OperationAnalysisContext context)
    {
        var syntax = (IConversionOperation)context.Operation;
        if (!syntax.IsImplicit)
            return;

        if (!syntax.Operand.ConstantValue.HasValue)
            return;

        if (syntax.Operand.ConstantValue.Value is not string)
            return;

        if (syntax.OperatorMethod?.ReturnType is not { } returnType || !returnType.ToString().StartsWith("ImSharp.Utf8StringHandler<"))
            return;

        var diagnostic = Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), returnType);
        context.ReportDiagnostic(diagnostic);
    }

    private static readonly DiagnosticDescriptor Rule = new("Luna01", "Prefer UTF8 Literals",
        "You are supplying a UTF16 literal to a Utf8StringHandler, prefer to supply a UTF8 literal",
        "Optimization", DiagnosticSeverity.Warning, true);
}
