using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Luna.Generators.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        if (!VerifySourceGenerators.Initialized)
            VerifySourceGenerators.Initialize();
        UseProjectRelativeDirectory(".verified");
    }

    public static Task VerifyCode<T>(string source) where T : IIncrementalGenerator, new()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var compilation = CSharpCompilation.Create("Tests", [syntaxTree])
            .AddReferences(MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(Type).Assembly.Location));
        var generator = new T();
        var driver    = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        return Verify(driver);
    }
}
