using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Luna.Generators;

internal static class ConfigPropertyAttribute
{
    public static CompilationUnitSyntax CreateAttribute()
    {
        var @namespace = SyntaxFactory.LunaGeneratorsNamespace();
        var embedded   = SyntaxFactory.Embedded();
        var generated  = SyntaxFactory.Generated();
        var comment = "/// <summary> Mark a field as a config property so that it invokes a save method and an event on changes. </summary>"
            .Comment();
        var usage = SyntaxFactory.AttributeUsage(AttributeTargets.Field);

        var nullableString = SyntaxFactory.NullableType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)));

        var propertyNameProperty = SyntaxFactory.CreateProperty("PropertyName",
            "The name of the generated property. If left null, the capitalized name of the field without leading underscore will be used.",
            nullableString);
        var eventNameProperty = SyntaxFactory.CreateProperty("EventName",
            "An optional name for an event invoked after the property is changed. If left null, the event is omitted.", nullableString);
        var saveMethodProperty = SyntaxFactory.CreateProperty("SaveMethodName",
                "The name of the save method to invoke on changes. If left null, \"Save\" will be used.", nullableString);
        var skipSaveProperty = SyntaxFactory.CreateProperty("SkipSave",
            "Whether to skip calling the save method entirely.", SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)));

        var attribute = SyntaxFactory.ClassDeclaration("ConfigPropertyAttribute".Identifier())
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InternalKeyword)))
            .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(embedded)),
                SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(generated)),
                SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(usage)))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("global::System.Attribute")))
            .AddMembers(propertyNameProperty, eventNameProperty, saveMethodProperty, skipSaveProperty)
            .WithLeadingTrivia(comment);

        return SyntaxFactory.CompilationUnit().AddMembers(@namespace.AddMembers(attribute)).Normalize();
    }
}
