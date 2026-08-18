using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Luna.Generators;

internal readonly struct DataAdapterMethodEntry(IMethodSymbol method, int id, bool disposeOnFailure)
{
    public IMethodSymbol Method { get; } = method;

    public int Id { get; } = id;

    public int Arity
        => Method.Parameters.Length;

    public int CombinedArity
        => IsFunction ? ~Arity : Arity;

    public bool IsFunction
        => !Method.ReturnsVoid;

    public bool DisposeOnFailure
        => disposeOnFailure;

    internal static bool TryCreate(SourceProductionContext context, IMethodSymbol method, out DataAdapterMethodEntry entry)
    {
        entry = default;
        if (method.MethodKind is not MethodKind.Ordinary and not MethodKind.PropertyGet)
            return DataAdapterValidation.Invalid(context, method, "only ordinary methods and property getters are supported");

        if (method.IsGenericMethod)
            return DataAdapterValidation.Invalid(context, method, "generic methods are not supported");

        if (method.Parameters.Length > 9)
            return DataAdapterValidation.Invalid(context, method, "at most 9 parameters are supported");

        if (method.Parameters.Any(static p => p.RefKind is not RefKind.None))
            return DataAdapterValidation.Invalid(context, method, "ref, in and out parameters are not supported");

        if (method.ReturnsByRef || method.ReturnsByRefReadonly)
            return DataAdapterValidation.Invalid(context, method, "ref returns are not supported");

        var attribute = method.GetAttributes()
            .FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == AdapterMethodAttribute.MetadataName);
        attribute ??= method.AssociatedSymbol?.GetAttributes()
            .FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == AdapterMethodAttribute.MetadataName);

        if (attribute?.ConstructorArguments.Length is not 1 || !TryGetMethodId(attribute.ConstructorArguments[0], out var id))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DataAdapterValidation.InvalidMethodId,
                method.Locations.FirstOrDefault(),
                method.ToDisplayString()));

            return false;
        }

        entry = new DataAdapterMethodEntry(method, id, attribute.GetNamedArgument("DisposeOnFailure") is true);
        return true;
    }

    private static bool TryGetMethodId(TypedConstant value, out int id)
    {
        id = 0;
        if (value.Value is null)
            return false;

        if (value.Kind is not (TypedConstantKind.Primitive or TypedConstantKind.Enum))
            return false;

        try
        {
            id = Convert.ToInt32(value.Value, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
