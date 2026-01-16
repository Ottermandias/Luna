using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Luna.Generators;

internal readonly record struct ConfigPropertyData(
    string FieldName,
    string PropertyName,
    string? EventName,
    string? SaveMethodName,
    string TypeName)
{
    public readonly string  FieldName      = FieldName;
    public readonly string  PropertyName   = PropertyName;
    public readonly string? EventName      = EventName;
    public readonly string? SaveMethodName = SaveMethodName;
    public readonly string  TypeName       = TypeName;

    public IEnumerable<MemberDeclarationSyntax> GetSyntax()
    {
        var setterStatements   = new List<StatementSyntax>();
        var fieldExpression    = FieldName.IdentifierName();
        var valueExpression    = "value".IdentifierName();
        var propertyExpression = PropertyName.Identifier();
        var typeSyntax         = SyntaxFactory.IdentifierName(TypeName);

        setterStatements.Add(SyntaxFactory.LocalDeclarationStatement(SyntaxFactory
            .VariableDeclaration(typeSyntax).AddVariables(SyntaxFactory
                .VariableDeclarator("__oldValue".Identifier())
                .WithInitializer(SyntaxFactory.EqualsValueClause(fieldExpression)))));
        setterStatements.Add(SyntaxFactory.ExpressionStatement(SyntaxFactory
            .InvocationExpression($"On{PropertyName}Changing".IdentifierName())
            .AddArgumentListArguments(SyntaxFactory.Argument(valueExpression), SyntaxFactory.Argument(fieldExpression))));

        setterStatements.Add(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
            fieldExpression, valueExpression)));

        setterStatements.Add(SyntaxFactory.ExpressionStatement(SyntaxFactory
            .InvocationExpression(SyntaxFactory.IdentifierName($"On{PropertyName}Changed"))
            .AddArgumentListArguments(SyntaxFactory.Argument(fieldExpression),
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("__oldValue")))));

        if (EventName is not null)
            setterStatements.Add(SyntaxFactory.ExpressionStatement(SyntaxFactory
                .InvocationExpression(SyntaxFactory.IdentifierName(EventName))
                .AddArgumentListArguments(SyntaxFactory.Argument(fieldExpression),
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("__oldValue")))));

        if (SaveMethodName is not null)
            setterStatements.Add(
                SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(SaveMethodName))));

        var ifStatement = SyntaxFactory.IfStatement(
            SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression,
                SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.GenericName(SyntaxFactory.Identifier("global::System.Collections.Generic.EqualityComparer"))
                                .AddTypeArgumentListArguments(typeSyntax), SyntaxFactory.IdentifierName("Default")),
                        SyntaxFactory.IdentifierName("Equals")))
                    .AddArgumentListArguments(SyntaxFactory.Argument(fieldExpression), SyntaxFactory.Argument(valueExpression))),
            SyntaxFactory.Block(setterStatements));
        var setter = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithBody(SyntaxFactory.Block(ifStatement));
        var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
            .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(fieldExpression))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        var propertySyntax = SyntaxFactory.PropertyDeclaration(typeSyntax, propertyExpression)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .AddAccessorListAccessors(getter, setter)
            .WithLeadingTrivia(SyntaxFactory.Comment($"/// <inheritdoc cref=\"{FieldName}\"/>"));

        yield return propertySyntax;

        var newValueParameter = SyntaxFactory.Parameter("newValue".Identifier()).WithType(typeSyntax);
        var oldValueParameter = SyntaxFactory.Parameter("oldValue".Identifier()).WithType(typeSyntax);
        var voidType          = SyntaxFactory.ParseTypeName("void");

        var partialChanging = SyntaxFactory.MethodDeclaration(voidType, $"On{PropertyName}Changing")
            .AddParameterListParameters(newValueParameter, oldValueParameter)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
            .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Generated())))
            .WithLeadingTrivia(SyntaxFactory.Comment($"/// <summary> Execute logic before <see cref=\"{PropertyName}\"/> changes. </summary>")
                , SyntaxFactory.Comment("/// <param name=\"newValue\"> The new value that is being set. </param>")
                , SyntaxFactory.Comment("/// <param name=\"oldValue\"> The current value that will be changed. </param>"))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        yield return partialChanging;

        var partialChanged = SyntaxFactory.MethodDeclaration(voidType, $"On{PropertyName}Changed")
            .AddParameterListParameters(newValueParameter, oldValueParameter)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
            .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Generated())))
            .WithLeadingTrivia(SyntaxFactory.Comment($"/// <summary> Execute logic after <see cref=\"{PropertyName}\"/> changed. </summary>")
                , SyntaxFactory.Comment("/// <param name=\"newValue\"> The new value that has been set. </param>")
                , SyntaxFactory.Comment("/// <param name=\"oldValue\"> The old value that has been changed. </param>"))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        yield return partialChanged;

        if (EventName is not null)
        {
            var @event = SyntaxFactory.EventDeclaration(SyntaxFactory.ParseTypeName($"global::System.Action<{TypeName}, {TypeName}>?"),
                    EventName.Identifier())
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Generated())))
                .WithLeadingTrivia(SyntaxFactory.Comment($"/// <summary> Invoked after <see cref=\"{PropertyName}\"/> changed. First argument is the new value, second is the old value. </summary>"))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            yield return @event;
        }
    }
}
