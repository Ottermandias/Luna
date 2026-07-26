using System.Text.Json.Serialization;

namespace Luna;

/// <summary> A JsonConverter attribute that allows for generic types. </summary>
/// <param name="genericTypeDefinition"> The generic type definition without explicit Type parameters. </param>
public sealed class GenericJsonConverterAttribute(Type genericTypeDefinition) : JsonConverterAttribute
{
    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert)
        => Activator.CreateInstance(genericTypeDefinition.MakeGenericType(typeToConvert.GenericTypeArguments)) as JsonConverter;
}
