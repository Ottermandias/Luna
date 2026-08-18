using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Luna.Generators;

public static class TypeUtility
{
    /// <summary> Convert a type symbol to a valid type syntax without relying on display strings. </summary>
    public static TypeSyntax GetTypeSyntax(this ITypeSymbol type)
    {
        var ret = type switch
        {
            IArrayTypeSymbol array => GetArrayTypeSyntax(array),
            IPointerTypeSymbol pointer => SyntaxFactory.PointerType(GetTypeSyntax(pointer.PointedAtType)),
            IFunctionPointerTypeSymbol function => GetFunctionPointerTypeSyntax(function),
            ITypeParameterSymbol parameter => parameter.Name.IdentifierName(),
            IErrorTypeSymbol => throw new InvalidOperationException($"Can not generate syntax for unresolved type '{type.ToDisplayString()}'."),
            INamedTypeSymbol { IsTupleType: true } named => GetTupleTypeSyntax(named),
            INamedTypeSymbol named => GetNamedTypeSyntax(named),
            _ when type.TypeKind is TypeKind.Dynamic => "dynamic".IdentifierName(),
            _ => throw new InvalidOperationException($"Unsupported adapter type '{type.ToDisplayString()}'."),
        };
        return ApplyNullableAnnotation(type, ret);
    }

    private static ArrayTypeSyntax GetArrayTypeSyntax(IArrayTypeSymbol array)
    {
        var dimensions = Enumerable.Range(0, array.Rank).Select(static ExpressionSyntax (_) => SyntaxFactory.OmittedArraySizeExpression());
        return SyntaxFactory.ArrayType(GetTypeSyntax(array.ElementType))
            .WithRankSpecifiers(SyntaxFactory.SingletonList(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SeparatedList(dimensions))));
    }

    private static TupleTypeSyntax GetTupleTypeSyntax(INamedTypeSymbol type)
    {
        return SyntaxFactory.TupleType(SyntaxFactory.SeparatedList(type.TupleElements.Select(static element =>
        {
            var elementType = GetTypeSyntax(element.Type);
            return element.IsExplicitlyNamedTupleElement
                ? SyntaxFactory.TupleElement(elementType, element.Name.Identifier())
                : SyntaxFactory.TupleElement(elementType);
        })));
    }

    private static TypeSyntax GetNamedTypeSyntax(INamedTypeSymbol type)
    {
        if (type.IsNativeIntegerType)
            return type.SpecialType switch
            {
                SpecialType.System_IntPtr  => "nint".IdentifierName(),
                SpecialType.System_UIntPtr => "nuint".IdentifierName(),
                _                          => throw new InvalidOperationException($"Unknown native integer type '{type}'."),
            };

        if (GetPredefinedType(type.SpecialType) is { } predefined)
            return predefined;

        // Nullable<T> should be emitted as T?
        if (type.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T)
            return SyntaxFactory.NullableType(GetTypeSyntax(type.TypeArguments[0]));

        return GetNamedTypeNameSyntax(type);
    }

    private static PredefinedTypeSyntax? GetPredefinedType(SpecialType type)
    {
        var kind = type switch
        {
            SpecialType.System_Object  => SyntaxKind.ObjectKeyword,
            SpecialType.System_Void    => SyntaxKind.VoidKeyword,
            SpecialType.System_Boolean => SyntaxKind.BoolKeyword,
            SpecialType.System_Char    => SyntaxKind.CharKeyword,
            SpecialType.System_SByte   => SyntaxKind.SByteKeyword,
            SpecialType.System_Byte    => SyntaxKind.ByteKeyword,
            SpecialType.System_Int16   => SyntaxKind.ShortKeyword,
            SpecialType.System_UInt16  => SyntaxKind.UShortKeyword,
            SpecialType.System_Int32   => SyntaxKind.IntKeyword,
            SpecialType.System_UInt32  => SyntaxKind.UIntKeyword,
            SpecialType.System_Int64   => SyntaxKind.LongKeyword,
            SpecialType.System_UInt64  => SyntaxKind.ULongKeyword,
            SpecialType.System_Decimal => SyntaxKind.DecimalKeyword,
            SpecialType.System_Single  => SyntaxKind.FloatKeyword,
            SpecialType.System_Double  => SyntaxKind.DoubleKeyword,
            SpecialType.System_String  => SyntaxKind.StringKeyword,

            _ => SyntaxKind.None,
        };

        return kind is SyntaxKind.None ? null : SyntaxFactory.PredefinedType(SyntaxFactory.Token(kind));
    }

    private static NameSyntax GetNamedTypeNameSyntax(INamedTypeSymbol type)
    {
        SimpleNameSyntax ownName;
        if (type.IsUnboundGenericType)
            ownName = SyntaxFactory.GenericName(type.Name.Identifier())
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(
                    Enumerable.Range(0, type.Arity).Select(static TypeSyntax (_) => SyntaxFactory.OmittedTypeArgument()))));
        else if (type.Arity is not 0)
            ownName = SyntaxFactory.GenericName(
                    type.Name.Identifier())
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(type.TypeArguments.Select(GetTypeSyntax))));
        else
            ownName = type.Name.IdentifierName();

        if (type.ContainingType is not null)
            return SyntaxFactory.QualifiedName(GetNamedTypeNameSyntax(type.ContainingType), ownName);

        if (type.ContainingNamespace.IsGlobalNamespace)
            return SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)), ownName);

        return SyntaxFactory.QualifiedName(GetGlobalNamespaceSyntax(type.ContainingNamespace), ownName);
    }

    private static TypeSyntax ApplyNullableAnnotation(ITypeSymbol symbol, TypeSyntax syntax)
    {
        if (symbol.NullableAnnotation is not NullableAnnotation.Annotated)
            return syntax;

        if (symbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T })
            return syntax;

        if (symbol.IsReferenceType || symbol is ITypeParameterSymbol || symbol.TypeKind is TypeKind.Dynamic)
            return SyntaxFactory.NullableType(syntax);

        return syntax;
    }

    private static FunctionPointerTypeSyntax GetFunctionPointerTypeSyntax(IFunctionPointerTypeSymbol type)
    {
        var signature  = type.Signature;
        var parameters = new FunctionPointerParameterSyntax[signature.Parameters.Length + 1];
        for (var i = 0; i < signature.Parameters.Length; ++i)
        {
            var param = signature.Parameters[i];
            parameters[i] = SyntaxFactory.FunctionPointerParameter(GetTypeSyntax(param.Type))
                .WithModifiers(GetFunctionPointerParameterModifiers(param.RefKind));
        }

        // The return type is the last "parameter" in delegate*<...>.
        parameters[signature.Parameters.Length] = SyntaxFactory.FunctionPointerParameter(GetTypeSyntax(signature.ReturnType))
            .WithModifiers(GetFunctionPointerReturnModifiers(signature.RefKind));
        var ret = SyntaxFactory.FunctionPointerType()
            .WithParameterList(SyntaxFactory.FunctionPointerParameterList(SyntaxFactory.SeparatedList(parameters)));

        if (GetFunctionPointerCallingConvention(signature) is { } convention)
            ret = ret.WithCallingConvention(convention);

        return ret;
    }

    private static SyntaxTokenList GetFunctionPointerParameterModifiers(RefKind refKind)
        => refKind switch
        {
            RefKind.None => default,
            RefKind.Ref  => SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword)),
            RefKind.Out  => SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.OutKeyword)),
            RefKind.In   => SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InKeyword)),
            RefKind.RefReadOnlyParameter => SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)),
            _ => throw new InvalidOperationException($"Unsupported function pointer parameter RefKind '{refKind}'."),
        };

    private static SyntaxTokenList GetFunctionPointerReturnModifiers(RefKind refKind)
        => refKind switch
        {
            RefKind.None => default,
            RefKind.Ref  => SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword)),
            RefKind.RefReadOnly => SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)),
            _ => throw new InvalidOperationException($"Unsupported function pointer return RefKind '{refKind}'."),
        };

    private static FunctionPointerCallingConventionSyntax?
        GetFunctionPointerCallingConvention(
            IMethodSymbol signature)
    {
        return signature.CallingConvention switch
        {
            SignatureCallingConvention.Default => null,
            SignatureCallingConvention.CDecl => GetUnmanagedCallingConvention("Cdecl"),
            SignatureCallingConvention.StdCall => GetUnmanagedCallingConvention("Stdcall"),
            SignatureCallingConvention.ThisCall => GetUnmanagedCallingConvention("Thiscall"),
            SignatureCallingConvention.FastCall => GetUnmanagedCallingConvention("Fastcall"),
            SignatureCallingConvention.Unmanaged => GetUnmanagedCallingConventionMulti(signature.UnmanagedCallingConventionTypes),
            _ => throw new InvalidOperationException($"Unsupported function pointer calling convention " + $"'{signature.CallingConvention}'."),
        };

        static FunctionPointerCallingConventionSyntax GetUnmanagedCallingConvention(string convention)
        {
            return SyntaxFactory.FunctionPointerCallingConvention(
                SyntaxFactory.Token(SyntaxKind.UnmanagedKeyword),
                SyntaxFactory.FunctionPointerUnmanagedCallingConventionList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.FunctionPointerUnmanagedCallingConvention(
                            SyntaxFactory.Identifier(convention)))));
        }

        static FunctionPointerCallingConventionSyntax GetUnmanagedCallingConventionMulti(IEnumerable<INamedTypeSymbol> conventions)
        {
            var names = conventions.Select(static convention =>
            {
                var name = convention.Name;
                if (name.StartsWith("CallConv", StringComparison.Ordinal))
                    name = name.Substring("CallConv".Length);
                return SyntaxFactory.FunctionPointerUnmanagedCallingConvention(SyntaxFactory.Identifier(name));
            }).ToArray();

            if (names.Length is 0)
                return SyntaxFactory.FunctionPointerCallingConvention(SyntaxFactory.Token(SyntaxKind.UnmanagedKeyword));

            return SyntaxFactory.FunctionPointerCallingConvention(SyntaxFactory.Token(SyntaxKind.UnmanagedKeyword),
                SyntaxFactory.FunctionPointerUnmanagedCallingConventionList(SyntaxFactory.SeparatedList(names)));
        }
    }

    private static NameSyntax GetGlobalNamespaceSyntax(INamespaceSymbol ns)
    {
        var parts = new List<string>();
        for (var current = ns; !current.IsGlobalNamespace; current = current.ContainingNamespace)
            parts.Add(current.Name);
        parts.Reverse();

        if (parts.Count is 0)
            throw new ArgumentException("The global namespace has no name.", nameof(ns));

        NameSyntax name = SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)),
            parts[0].IdentifierName());
        for (var i = 1; i < parts.Count; ++i)
            name = SyntaxFactory.QualifiedName(name, parts[i].IdentifierName());

        return name;
    }
}
