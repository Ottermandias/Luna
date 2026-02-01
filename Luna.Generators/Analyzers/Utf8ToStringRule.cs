using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#pragma warning disable RS2008

namespace Luna.Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Utf8ToStringRule : DiagnosticAnalyzer
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

        if (syntax.Operand is not IInvocationOperation invocation)
            return;

        if (invocation.TargetMethod.Name is not nameof(object.ToString))
            return;

        if (invocation.TargetMethod.Parameters.Length > 0)
            return;

        if (syntax.OperatorMethod?.ReturnType is not { } returnType || !returnType.ToString().StartsWith("ImSharp.Utf8StringHandler<"))
            return;

        var diagnostic = Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }

    private static readonly DiagnosticDescriptor Rule = new("Luna03", "Prefer passing interpolated strings to ToString calls",
        "You are calling ToString on an object passed to an Utf8StringHandler, which may be better optimized when passing an interpolated string instead",
        "Optimization", DiagnosticSeverity.Warning, true);
}
