using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Luna.Generators;

internal readonly struct DataAdapterMethodEntry(
    IMethodSymbol method,
    int id,
    bool disposeOnFailure,
    bool alwaysAlive,
    string? subscriptionEvent,
    string? unsubscriptionEvent,
    INamedTypeSymbol? enumType,
    IFieldSymbol? enumMember)
{
    public readonly IMethodSymbol     Method       = method;
    public readonly int               Id           = id;
    public readonly INamedTypeSymbol? IdEnumType   = enumType;
    public readonly IFieldSymbol?     IdEnumMember = enumMember;

    public readonly string SubscriptionEvent   = subscriptionEvent ?? string.Empty;
    public readonly string UnsubscriptionEvent = unsubscriptionEvent ?? string.Empty;

    public int Arity
        => IsEvent ? 2 : Method.Parameters.Length;

    public bool IsEvent
        => Method.MethodKind is MethodKind.EventAdd;

    public int CombinedArity
        => IsFunction ? ~Arity : Arity;

    public bool IsFunction
        => !Method.ReturnsVoid;

    public bool DisposeOnFailure
        => disposeOnFailure;

    public bool AlwaysAlive
        => alwaysAlive;


    internal static bool TryCreate(SourceProductionContext context, IMethodSymbol method, out DataAdapterMethodEntry entry)
    {
        entry = default;
        if (method.MethodKind is not MethodKind.Ordinary and not MethodKind.PropertyGet and not MethodKind.EventAdd)
            return DataAdapterValidation.Invalid(context, method, "only ordinary methods, events, and property getters are supported");

        if (method.IsGenericMethod)
            return DataAdapterValidation.Invalid(context, method, "generic methods are not supported");

        if (method.Parameters.Length > 9)
            return DataAdapterValidation.Invalid(context, method, "at most 9 parameters are supported");

        if (method.Parameters.Any(static p => p.RefKind is not RefKind.None))
            return DataAdapterValidation.Invalid(context, method, "ref, in and out parameters are not supported");

        if (method.ReturnsByRef || method.ReturnsByRefReadonly)
            return DataAdapterValidation.Invalid(context, method, "ref returns are not supported");

        if (method.MethodKind is MethodKind.EventAdd)
        {
            var @event = (IEventSymbol)method.AssociatedSymbol!;
            if (!@event.DeclaringSyntaxReferences.Any(static r => r.GetSyntax() is VariableDeclaratorSyntax
                {
                    Parent.Parent: EventFieldDeclarationSyntax,
                }))
                return DataAdapterValidation.Invalid(context, method, "only field-like events are supported");
        }

        var attribute = method.GetAttributes()
            .FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == AdapterMethodAttribute.MetadataName);
        attribute ??= method.AssociatedSymbol?.GetAttributes()
            .FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == AdapterMethodAttribute.MetadataName);

        if (attribute?.ConstructorArguments.Length is not 1
         || !TryGetMethodId(attribute.ConstructorArguments[0], out var id, out var enumType, out var enumMember))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DataAdapterValidation.InvalidMethodId,
                method.Locations.FirstOrDefault(),
                method.ToDisplayString()));

            return false;
        }

        entry = new DataAdapterMethodEntry(method, id,
            attribute.GetNamedArgument("DisposeOnFailure") is true,
            attribute.GetNamedArgument("AlwaysAlive") is true,
            attribute.GetNamedArgument("SubscribeEvent") as string,
            attribute.GetNamedArgument("UnsubscribeEvent") as string,
            enumType, enumMember);
        return true;
    }

    private static bool TryGetMethodId(TypedConstant value, out int id, out INamedTypeSymbol? enumType, out IFieldSymbol? enumMember)
    {
        id         = 0;
        enumType   = null;
        enumMember = null;

        if (value.Value is null)
            return false;

        if (value.Kind is not (TypedConstantKind.Primitive or TypedConstantKind.Enum))
            return false;

        try
        {
            id = Convert.ToInt32(value.Value, CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            return false;
        }

        if (value.Kind is not TypedConstantKind.Enum || value.Type is not INamedTypeSymbol type)
            return true;

        var matchingId = id;
        enumType = type;
        enumMember = type.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(f
                => f.HasConstantValue
             && f.ConstantValue is not null
             && Convert.ToInt32(f.ConstantValue, CultureInfo.InvariantCulture) == matchingId);

        if (enumMember is null)
            enumType = null;
        return true;
    }
}
