using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable RS2008

namespace Luna.Generators;

internal static class DataAdapterValidation
{
    internal static bool ValidateContainingType(SourceProductionContext context, INamedTypeSymbol type)
    {
        if (type.TypeKind is not TypeKind.Class)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidSignature,
                type.Locations.FirstOrDefault(),
                type.Name,
                "the containing adapter type must be a class"));

            return false;
        }

        for (var current = type; current is not null; current = current.ContainingType)
        {
            foreach (var syntaxReference in current.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax() is not TypeDeclarationSyntax declaration)
                    continue;

                if (declaration.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword)))
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    TypeMustBePartial,
                    declaration.Identifier.GetLocation(),
                    type.ToDisplayString()));

                return false;
            }
        }

        return true;
    }

    internal static void CheckUniqueness(SourceProductionContext context, List<DataAdapterMethodEntry> entries)
    {
        var ret = new HashSet<(int Arity, int Id)>();
        for (var i = 0; i < entries.Count; ++i)
        {
            var entry = entries[i];
            if (ret.Add((entry.IsFunction ? ~entry.Arity : entry.Arity, entry.Id)))
                continue;

            context.ReportDiagnostic(Diagnostic.Create(
                DuplicateMethod,
                entry.Method.Locations.FirstOrDefault(),
                entry.Id,
                entry.Arity,
                entry.IsFunction ? "function" : "action"));
            entries.RemoveAt(i--);
        }
    }

    internal static bool Invalid(
        SourceProductionContext context,
        IMethodSymbol method,
        string reason)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            InvalidSignature,
            method.Locations.FirstOrDefault(),
            method.ToDisplayString(),
            reason));
        return false;
    }


    private static readonly DiagnosticDescriptor InvalidSignature = new(
        "LUNAIPC001",
        "Invalid adapter method",
        "Method '{0}' can not be exposed as an adapter method: {1}",
        "Luna.Ipc",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor TypeMustBePartial = new(
        "LUNAIPC002",
        "Adapter type must be partial",
        "Type '{0}' and all containing types must be partial in order to generate adapter methods",
        "Luna.Ipc",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor DuplicateMethod = new(
        "LUNAIPC003",
        "Duplicate adapter method",
        "Adapter method ID {0} is already used for a {1}-argument {2}",
        "Luna.Ipc",
        DiagnosticSeverity.Error,
        true);

    internal static readonly DiagnosticDescriptor InvalidMethodId = new(
        "LUNAIPC004",
        "Invalid adapter method ID",
        "The adapter method ID on '{0}' must be an integral constant or enum value representable by Int32",
        "Luna.Ipc",
        DiagnosticSeverity.Error,
        true);
}
