using Microsoft.CodeAnalysis;

namespace Luna.Generators;

[Generator]
public sealed class LunaGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(i => i.AddEmbeddedAttributeDefinition());
    }
}
