using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#pragma warning disable RS2008

namespace Luna.Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EnumValuesRule : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(AnalyzeMethod, OperationKind.Invocation);
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Rule];

    private static void AnalyzeMethod(OperationAnalysisContext context)
    {
        var operation = (IInvocationOperation)context.Operation;
        if (operation.IsImplicit)
            return;

        if (!operation.TargetMethod.IsGenericMethod)
            return;

        if (!operation.TargetMethod.IsStatic)
            return;

        if (operation.TargetMethod.Name != nameof(Enum.GetValues))
            return;

        if (!SymbolEqualityComparer.Default.Equals(operation.TargetMethod.ContainingType,
                context.Compilation.GetTypeByMetadataName("System.Enum")))
            return;

        var diagnostic = Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), operation.TargetMethod.TypeArguments[0].Name);
        context.ReportDiagnostic(diagnostic);
    }

    private static readonly DiagnosticDescriptor Rule = new("Luna02", "Prefer Values extension property",
        "Enum.GetValues<{0}>() uses reflection to iterate the values. This is unnecessary overhead.",
        "Optimization", DiagnosticSeverity.Warning, true);
}
