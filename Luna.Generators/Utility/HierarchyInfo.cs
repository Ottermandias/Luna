using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Luna.Generators;

internal readonly record struct TypeInfo(string QualifiedName, TypeKind Kind, bool IsRecord)
{
    public readonly string   QualifiedName = QualifiedName;
    public readonly TypeKind Kind          = Kind;
    public readonly bool     IsRecord      = IsRecord;

    public TypeDeclarationSyntax GetSyntax()
        => Kind switch
        {
            TypeKind.Struct    => SyntaxFactory.StructDeclaration(QualifiedName),
            TypeKind.Interface => SyntaxFactory.InterfaceDeclaration(QualifiedName),
            TypeKind.Class when IsRecord => SyntaxFactory.RecordDeclaration(SyntaxFactory.Token(SyntaxKind.RecordKeyword), QualifiedName)
                .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
                .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken)),
            _ => SyntaxFactory.ClassDeclaration(QualifiedName),
        };
}

internal readonly record struct HierarchyInfo(string FilenameHint, string MetadataName, string Namespace, ValueCollection<TypeInfo> Hierarchy)
{
    public readonly string                    FilenameHint = FilenameHint;
    public readonly string                    MetadataName = MetadataName;
    public readonly string                    Namespace    = Namespace;
    public readonly ValueCollection<TypeInfo> Hierarchy    = Hierarchy;

    public static HierarchyInfo From(INamedTypeSymbol typeSymbol)
    {
        var list = new List<TypeInfo>();
        for (var parent = typeSymbol; parent is not null; parent = parent.ContainingType)
            list.Add(new TypeInfo(parent.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), parent.TypeKind, parent.IsRecord));

        return new HierarchyInfo(
            typeSymbol.FullyQualifiedMetadataName(),
            typeSymbol.MetadataName,
            typeSymbol.ContainingNamespace.ToDisplayString(
                new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)),
            new ValueCollection<TypeInfo>(list));
    }

    public CompilationUnitSyntax GetCompilationUnit(params IEnumerable<MemberDeclarationSyntax> members)
    {
        var type = Hierarchy[0].GetSyntax().AddInheritCommentPartial().AddMembers(members.ToArray());
        foreach (var parent in Hierarchy.Skip(1))
            type = parent.GetSyntax().AddInheritCommentPartial().AddMembers(type);

        var syntax = SyntaxFactory.DefaultFileTrivia();
        if (Namespace.Length is 0)
        {
            syntax = syntax.Add(SyntaxFactory.Inheritdoc());
            return SyntaxFactory.CompilationUnit().AddMembers(type.WithLeadingTrivia(syntax)).Normalize();
        }

        return SyntaxFactory.CompilationUnit().AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(Namespace))
            .WithLeadingTrivia(syntax)
            .AddMembers(type))
            .Normalize();
    }
}
