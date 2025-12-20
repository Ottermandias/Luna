using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Luna.Generators;

[Generator]
public sealed class IpcGenerator : IIncrementalGenerator
{
    private const string IDisposableName = "System.IDisposable";

    private const string IDalamudPluginInterfaceName = "Dalamud.Plugin.IDalamudPluginInterface";

    #region Embedded Attributes

    private static readonly string GeneratedIpcSubscriberAttribute = IndentedStringBuilder.CreatePreamble()
        .OpenNamespace("Luna.Generators")
        .AppendLine(
            "/// <summary> Instructs Luna's source generator to generate an implementation of the returned type that performs Dalamud inter-plugin calls. </summary>")
        .AppendLine("/// <remarks>")
        .AppendLine("/// Use with <see cref=\"Luna.Generators.IpcAttribute\"/> and <see cref=\"Luna.Generators.EraseTypeAttribute\"/>.<para />")
        .AppendLine(
            $"/// Methods marked with this attribute must be static, partial, take a single <see cref=\"{IDalamudPluginInterfaceName}\"/> parameter, and return an interface or an abstract class describing the desired IPC contract.")
        .AppendLine("/// </remarks>")
        .AppendLine("[AttributeUsage(AttributeTargets.Method)]")
        .EmbeddedAttribute()
        .GeneratedAttribute()
        .AppendLine($"internal sealed class {nameof(GeneratedIpcSubscriberAttribute)} : Attribute")
        .OpenBlock()
        .AppendLine(
            "/// <summary> Whether the underlying IPC subscribers shall be initialized lazily on first call, instead of eagerly on construction. </summary>")
        .AppendLine("public bool LazySubscribers { get; set; } = false;")
        .CloseBlock().AppendLine()
        .CloseAllBlocks()
        .ToString();

    private static readonly string GeneratedIpcProviderAttribute = IndentedStringBuilder.CreatePreamble()
        .OpenNamespace("Luna.Generators")
        .AppendLine(
            "/// <summary> Instructs Luna's source generator to generate a Dalamud inter-plugin call provider that calls a given implementation. </summary>")
        .AppendLine("/// <remarks>")
        .AppendLine("/// Use with <see cref=\"Luna.Generators.IpcAttribute\"/> and <see cref=\"Luna.Generators.EraseTypeAttribute\"/>.<para />")
        .AppendLine(
            $"/// Methods marked with this attribute must be static, partial, take two parameters (a <see cref=\"{IDalamudPluginInterfaceName}\"/> and an implementation of the desired IPC contract, in this order), and return an <see cref=\"{IDisposableName}\"/>.")
        .AppendLine("/// </remarks>")
        .AppendLine("[AttributeUsage(AttributeTargets.Method)]")
        .EmbeddedAttribute()
        .GeneratedAttribute()
        .AppendLine($"internal sealed class {nameof(GeneratedIpcProviderAttribute)} : Attribute;")
        .CloseAllBlocks()
        .ToString();

    private static readonly string IpcAttribute = IndentedStringBuilder.CreatePreamble()
        .OpenNamespace("Luna.Generators")
        .AppendLine("/// <summary> Declares the marked method or event as part of the IPC contract. </summary>")
        .AppendLine("/// <param name=\"name\"> The name of the IPC corresponding to the marked method or event. </param>")
        .AppendLine("/// <remarks> This cannot be applied to properties themselves, but can be applied to their getters or setters. </remarks>")
        .AppendLine("[AttributeUsage(AttributeTargets.Method | AttributeTargets.Event)]")
        .EmbeddedAttribute()
        .GeneratedAttribute()
        .AppendLine($"internal sealed class {nameof(IpcAttribute)}(string name) : Attribute")
        .OpenBlock()
        .AppendLine("/// <summary> The name of the IPC corresponding to the marked method or event. </summary>")
        .AppendLine("public string Name => name;")
        .CloseAllBlocks()
        .ToString();

    private static readonly string EraseTypeAttribute = IndentedStringBuilder.CreatePreamble()
        .OpenNamespace("Luna.Generators")
        .AppendLine("/// <summary> Erases the custom type of the marked parameter or return value at the IPC level, replacing it by the best matching shared type the generator can determine. Not currently supported on events. </summary>")
        .AppendLine("/// <remarks> Enums and strong types (see <see cref=\"Luna.Generators.StrongTypeAttribute\"/>) will be replaced by their underlying type. If no better match can be made, types will be replaced by <see cref=\"System.Object\"/>. </remarks>")
        .AppendLine("[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]")
        .EmbeddedAttribute()
        .GeneratedAttribute()
        .AppendLine($"internal sealed class {nameof(EraseTypeAttribute)} : Attribute")
        .OpenBlock()
        .AppendLine("/// <summary> A method to use, instead of a cast, to marshal objects of custom types into objects of shared types. </summary>")
        .AppendLine("/// <remarks>")
        .AppendLine("/// Only pass a method name to use a static method from the custom type.<para />")
        .AppendLine("/// Prefix with a <c>.</c> to use a member expression.<para />")
        .AppendLine("/// Pass a type and a method (<c>Type.Method</c>) to use a static method from the given type.")
        .AppendLine("/// </remarks>")
        .AppendLine("public string Marshal { get; set; } = string.Empty;")
        .AppendLine()
        .AppendLine("/// <summary> A method to use, instead of a cast, to marshal objects of shared types into objects of custom types. </summary>")
        .AppendLine("/// <remarks>")
        .AppendLine("/// Only pass a method name to use a static method from the custom type.<para />")
        .AppendLine("/// Prefix with a <c>.</c> to use a member expression.<para />")
        .AppendLine("/// Pass a type and a method (<c>Type.Method</c>) to use a static method from the given type.<para />")
        .AppendLine("/// Pass <c>new</c> to use a constructor of the custom type that takes a single parameter.")
        .AppendLine("/// </remarks>")
        .AppendLine("public string MarshalBack { get; set; } = string.Empty;")
        .CloseBlock().AppendLine()
        .AppendLine()
        .AppendLine("/// <summary> Erases the custom type of the marked parameter or return value at the IPC level, replacing it by the given (preferably shared) type. </summary>")
        .AppendLine("/// <typeparam name=\"T\"> The type to use at the IPC level. </typeparam>")
        .AppendLine("[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]")
        .EmbeddedAttribute()
        .GeneratedAttribute()
        .AppendLine($"internal sealed class {nameof(EraseTypeAttribute)}<T> : Attribute")
        .OpenBlock()
        .AppendLine("/// <summary> A method to use, instead of a cast, to marshal objects of custom types into objects of shared types. </summary>")
        .AppendLine("/// <remarks>")
        .AppendLine("/// Only pass a method name to use a static method from the custom type.<para />")
        .AppendLine("/// Prefix with a <c>.</c> to use a member expression.<para />")
        .AppendLine("/// Pass a type and a method (<c>Type.Method</c>) to use a static method from the given type.")
        .AppendLine("/// </remarks>")
        .AppendLine("public string Marshal { get; set; } = string.Empty;")
        .AppendLine()
        .AppendLine("/// <summary> A method to use, instead of a cast, to marshal objects of shared types into objects of custom types. </summary>")
        .AppendLine("/// <remarks>")
        .AppendLine("/// Only pass a method name to use a static method from the custom type.<para />")
        .AppendLine("/// Prefix with a <c>.</c> to use a member expression.<para />")
        .AppendLine("/// Pass a type and a method (<c>Type.Method</c>) to use a static method from the given type.<para />")
        .AppendLine("/// Pass <c>new</c> to use a constructor of the custom type that takes a single parameter.")
        .AppendLine("/// </remarks>")
        .AppendLine("public string MarshalBack { get; set; } = string.Empty;")
        .CloseBlock().AppendLine()
        .CloseAllBlocks()
        .ToString();

    #endregion

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Utility.AddTagAttributes(ref context, GeneratedIpcSubscriberAttribute, nameof(GeneratedIpcSubscriberAttribute));
        Utility.AddTagAttributes(ref context, GeneratedIpcProviderAttribute,   nameof(GeneratedIpcProviderAttribute));
        Utility.AddTagAttributes(ref context, IpcAttribute,                    nameof(IpcAttribute));
        Utility.AddTagAttributes(ref context, EraseTypeAttribute,              nameof(EraseTypeAttribute));

        Utility.Generate(ref context, nameof(GeneratedIpcSubscriberAttribute), TransformIpcSubscriber,
            static (context, info) => context.AddSource($"{info.DeclaringType.FullyQualified}.{info.MethodName}.g.cs",
                GenerateSubscriberPartialClass(info)));

        Utility.Generate(ref context, nameof(GeneratedIpcProviderAttribute), TransformIpcProvider,
            static (context, info)
                => context.AddSource($"{info.DeclaringType.FullyQualified}.{info.MethodName}.g.cs", GenerateProviderPartialClass(info)));
    }

    #region Parsing

    private static IpcProviderOrSubscriberInfo? TransformIpcSubscriber(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        if (context.TargetSymbol is not IMethodSymbol
            {
                MethodKind         : MethodKind.Ordinary,
                IsStatic           : true,
                IsPartialDefinition: true,
                ContainingType     : not null,
                Parameters.Length  : 1,
                ReturnType:
                {
                    TypeKind: TypeKind.Interface,
                } or
                {
                    TypeKind: TypeKind.Class,
                    IsSealed: false,
                    IsStatic: false,
                },
            } methodSymbol)
            return null;

        var dalamudPluginInterface = context.SemanticModel.Compilation.GetTypeByMetadataName(IDalamudPluginInterfaceName);
        if (!methodSymbol.Parameters[0].Type.Equals(dalamudPluginInterface, SymbolEqualityComparer.Default))
            return null;

        var lazy = (bool)(Utility.GetAttributeNamedArgument(context.Attributes[0], "LazySubscribers") ?? false);

        return ParseProviderOrSubscriber(methodSymbol, context.SemanticModel.Compilation, false, lazy, token);
    }

    private static IpcProviderOrSubscriberInfo? TransformIpcProvider(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        if (context.TargetSymbol is not IMethodSymbol
            {
                MethodKind         : MethodKind.Ordinary,
                IsStatic           : true,
                IsPartialDefinition: true,
                ContainingType     : not null,
                Parameters.Length  : 2,
            } methodSymbol)
            return null;

        var disposable = context.SemanticModel.Compilation.GetTypeByMetadataName(IDisposableName);
        if (!methodSymbol.ReturnType.Equals(disposable, SymbolEqualityComparer.Default))
            return null;

        var dalamudPluginInterface = context.SemanticModel.Compilation.GetTypeByMetadataName(IDalamudPluginInterfaceName);
        if (!methodSymbol.Parameters[0].Type.Equals(dalamudPluginInterface, SymbolEqualityComparer.Default))
            return null;

        if (!methodSymbol.Parameters[1].Type.Equals(methodSymbol.ContainingType, SymbolEqualityComparer.Default))
            return null;

        return ParseProviderOrSubscriber(methodSymbol, context.SemanticModel.Compilation, true, false, token);
    }

    private static string GetIpcName(ISymbol symbol, INamedTypeSymbol ipcAttribute)
    {
        var attribute = Utility.FindAttribute(symbol, ipcAttribute);
        if (attribute is null || attribute.ConstructorArguments.Length < 1)
            return string.Empty;

        return attribute.ConstructorArguments[0].Value as string ?? string.Empty;
    }

    private static IpcProviderOrSubscriberInfo ParseProviderOrSubscriber(IMethodSymbol methodSymbol, Compilation compilation, bool provider,
        bool lazy, CancellationToken token)
    {
        var methods    = new List<MethodInfo>();
        var properties = new List<PropertyInfo>();
        var events     = new List<EventInfo>();

        CollectMembers(provider ? methodSymbol.Parameters[1].Type : methodSymbol.ReturnType, compilation, provider, methods, properties, events,
            token);

        return new IpcProviderOrSubscriberInfo(methodSymbol.ContainingType!.ToString(), methodSymbol.ContainingType!.TypeKind,
            Utility.GetFullNamespace(methodSymbol.ContainingType), methodSymbol.Name, methodSymbol.ReturnType.ToString(),
            methodSymbol.DeclaredAccessibility, Utility.IsNew(methodSymbol, token), lazy, methodSymbol.Parameters[0].Name,
            provider ? new(methodSymbol.Parameters[1].Name, methodSymbol.Parameters[1].Type) : ParameterInfo.Void, methods, properties, events);
    }

    private static void CollectMembers(ITypeSymbol typeSymbol, Compilation compilation, bool provider, List<MethodInfo> methods,
        List<PropertyInfo> properties, List<EventInfo> events, CancellationToken token)
    {
        if (typeSymbol.BaseType is { } baseTypeSymbol)
            CollectMembers(baseTypeSymbol, compilation, provider, methods, properties, events, token);

        foreach (var ifaceSymbol in typeSymbol.Interfaces)
            CollectMembers(ifaceSymbol, compilation, provider, methods, properties, events, token);

        var ipcAttribute = compilation.GetTypeByMetadataName($"Luna.Generators.{nameof(IpcAttribute)}")!;
        foreach (var symbol in typeSymbol.GetMembers())
        {
            if (!provider && typeSymbol.TypeKind is TypeKind.Class && !symbol.IsAbstract)
                continue;

            switch (symbol)
            {
                case IMethodSymbol methodSymbol:
                    if (methodSymbol.MethodKind is not MethodKind.Ordinary)
                        break;

                    var ipcName = GetIpcName(methodSymbol, ipcAttribute);
                    if (ipcName.Length == 0)
                        break;

                    methods.Add(new(methodSymbol.Name, ParseReturnParameter(methodSymbol, compilation),
                        ipcName, methodSymbol.DeclaredAccessibility,
                        ParseParameters(methodSymbol.Parameters, compilation)));
                    break;
                case IPropertySymbol propertySymbol:
                    var getIpcName = propertySymbol.GetMethod switch
                    {
                        null          => string.Empty,
                        var getSymbol => GetIpcName(getSymbol, ipcAttribute),
                    };
                    var setIpcName = propertySymbol.SetMethod switch
                    {
                        null          => string.Empty,
                        var setSymbol => GetIpcName(setSymbol, ipcAttribute),
                    };

                    if (getIpcName.Length == 0 && setIpcName.Length == 0)
                        break;

                    properties.Add(new(propertySymbol.Name,
                        ParsePropertyValueParameter(propertySymbol, compilation), getIpcName, setIpcName,
                        propertySymbol.DeclaredAccessibility, propertySymbol.GetMethod?.DeclaredAccessibility ?? Accessibility.NotApplicable,
                        propertySymbol.SetMethod?.DeclaredAccessibility ?? Accessibility.NotApplicable,
                        ParseParameters(propertySymbol.Parameters, compilation)));
                    break;
                case IEventSymbol eventSymbol:
                    if (eventSymbol.Type is not INamedTypeSymbol namedEventTypeSymbol)
                        break;

                    var eventType     = namedEventTypeSymbol.ToString();
                    var pos           = eventType.IndexOf('<');
                    var eventBaseType = pos < 0 ? eventType : eventType.Substring(0, pos);
                    if (eventBaseType is not "System.Action")
                        break;

                    ipcName = GetIpcName(eventSymbol, ipcAttribute);
                    if (ipcName.Length == 0)
                        break;

                    events.Add(new(eventSymbol.Name, eventType, ipcName, eventSymbol.DeclaredAccessibility,
                        ImmutableArrayExtensions
                            .Select(namedEventTypeSymbol.TypeArguments, static param => new ParameterInfo(string.Empty, param)).ToArray()));
                    break;
            }

            token.ThrowIfCancellationRequested();
        }
    }

    private static ParameterInfo[] ParseParameters(ImmutableArray<IParameterSymbol> parameters, Compilation compilation)
        => ImmutableArrayExtensions.Select(parameters,
                param => BuildParameterInfo(param.Type, param.GetAttributes(), param.Name, compilation))
            .ToArray();

    private static ParameterInfo ParseReturnParameter(IMethodSymbol methodSymbol, Compilation compilation)
        => BuildParameterInfo(methodSymbol.ReturnType, methodSymbol.GetReturnTypeAttributes(), "return", compilation);

    private static ParameterInfo ParsePropertyValueParameter(IPropertySymbol propertySymbol, Compilation compilation)
        => BuildParameterInfo(propertySymbol.Type, propertySymbol.GetAttributes(), "value", compilation);

    private static ParameterInfo BuildParameterInfo(ITypeSymbol type, ImmutableArray<AttributeData> attributes, string name,
        Compilation compilation)
    {
        var eraseType =
            Utility.FindGenericAttributes(compilation, attributes, $"Luna.Generators.{nameof(EraseTypeAttribute)}`1").FirstOrDefault()
         ?? Utility.FindAttribute(compilation, attributes, $"Luna.Generators.{nameof(EraseTypeAttribute)}");
        if (eraseType is null)
            return new(name, type);

        var marshal     = Utility.GetAttributeNamedArgument(eraseType, "Marshal") as string ?? string.Empty;
        var marshalBack = Utility.GetAttributeNamedArgument(eraseType, "MarshalBack") as string ?? string.Empty;

        if (eraseType.AttributeClass is { TypeArguments.Length: 1, })
            return new(name, type, eraseType.AttributeClass.TypeArguments[0], marshal, marshalBack);

        if (Utility.FindGenericAttributes(compilation, type, "Luna.Generators.StrongTypeAttribute`1").FirstOrDefault() is
            { AttributeClass.TypeArguments.Length: 1, ConstructorArguments.Length: >= 1, } strongType)
            return new(name, type, strongType.AttributeClass.TypeArguments[0],
                marshal.Length > 0 ? marshal : $".{strongType.ConstructorArguments[0].Value}", marshalBack.Length > 0 ? marshalBack : "new");

        return type switch
        {
            INamedTypeSymbol
            {
                EnumUnderlyingType: { } underlyingType,
            } => new(name, type, underlyingType, marshal, marshalBack),
            IPointerTypeSymbol => new(name, type, "nint", marshal, marshalBack),
            _                  => new(name, type, "object", marshal, marshalBack),
        };
    }

    #endregion

    #region Rendering

    private static SourceText GenerateSubscriberPartialClass(in IpcProviderOrSubscriberInfo info)
    {
        var builder = IndentedStringBuilder.CreatePreamble()
            .OpenNamespace(info.DeclaringTypeNamespace);

        builder.AppendLine($"partial {info.DeclaringTypeKind.ToKeyword()} {info.DeclaringType.Name}");
        builder.OpenBlock();
        builder.GeneratedAttribute();
        builder.AppendLine(
            $"{info.Accessibility.ToModifier()}{(info.IsNew ? "new " : string.Empty)}static partial {info.ReturnType} {info.MethodName}(global::{IDalamudPluginInterfaceName} {info.PluginInterfaceName})");
        builder.AppendLine($"    => new {info.DeclaringType.Name}_{info.MethodName}_SubscriberImplementation({info.PluginInterfaceName});");
        builder.CloseBlock().AppendLine();

        var allNames = new HashSet<string>();
        allNames.Add($"{info.DeclaringType.Name}_{info.MethodName}_SubscriberImplementation");
        allNames.Add("_pi");
        allNames.Add("pi");
        AddExposedMemberNames(allNames, info);
        PrepareDeclarationLists(info, out var getterDecls, out var setterDecls, out var eventDecls, out var methodDecls);

        builder.AppendLine();
        builder.GeneratedAttribute();
        builder.AppendLine($"file sealed class {info.DeclaringType.Name}_{info.MethodName}_SubscriberImplementation : {info.ReturnType}");
        builder.OpenBlock();
        builder.AppendLine($"private readonly global::{IDalamudPluginInterfaceName} _pi;");
        builder.AppendLine();
        AppendAllDeclarations(builder, info, false, allNames, getterDecls, setterDecls, eventDecls, methodDecls);
        builder.AppendLine();
        builder.AppendLine(
            $"public {info.DeclaringType.Name}_{info.MethodName}_SubscriberImplementation(global::{IDalamudPluginInterfaceName} pi)");
        builder.OpenBlock();
        builder.AppendLine("_pi = pi;");
        AppendAllInitializations(builder, info, false, getterDecls, setterDecls, eventDecls, methodDecls);
        builder.CloseBlock().AppendLine();

        var i = -1;
        foreach (var property in info.Properties)
        {
            ++i;
            builder.AppendLine();
            builder.Append(
                $"{(info.DeclaringTypeKind is TypeKind.Class ? property.Accessibility.ToModifier() + "override" : "public")} {property.ValueParameter.Type} {(property.IsIndexer ? "this" : property.Name)}");
            if (property.Indices.Count > 0)
            {
                builder.Append(
                    $"[{string.Join(", ", property.Indices.Select(static index => $"{index.Type} {index.Name}"))}]"
                );
            }

            if (!info.Lazy
             && property.GetIpcName.Length > 0
             && property.SetIpcName.Length == 0
             && (info.DeclaringTypeKind is not TypeKind.Class
                 || property.GetAccessibility == property.Accessibility))
            {
                AppendCall(builder, getterDecls[i], property.GetIpcName, false, false, property.ValueParameter, property.Indices,
                    string.Empty);
            }
            else
            {
                builder.AppendLine();
                builder.OpenBlock();
                if (property.GetIpcName.Length > 0)
                {
                    builder.Append(
                        $"{(info.DeclaringTypeKind is TypeKind.Class && property.GetAccessibility != property.Accessibility ? property.GetAccessibility.ToModifier() : string.Empty)}get");
                    AppendCall(builder, getterDecls[i], property.GetIpcName, info.Lazy, true, property.ValueParameter, property.Indices,
                        string.Empty);
                }

                if (property.SetIpcName.Length > 0)
                {
                    builder.Append(
                        $"{(info.DeclaringTypeKind is TypeKind.Class && property.SetAccessibility != property.Accessibility ? property.SetAccessibility.ToModifier() : string.Empty)}set");
                    AppendCall(builder, setterDecls[i], property.SetIpcName, info.Lazy, true, ParameterInfo.Void, property.Indices,
                        property.ValueParameter.GetMarshalExpression("value"));
                }

                builder.CloseBlock().AppendLine();
            }
        }

        i = -1;
        foreach (var @event in info.Events)
        {
            ++i;
            builder.AppendLine();
            builder.AppendLine(
                $"{(info.DeclaringTypeKind is TypeKind.Class ? @event.Accessibility.ToModifier() + "override" : "public")} event {@event.Type} {@event.Name}");
            builder.OpenBlock();
            builder.Append("add");
            AppendAnyCall(builder, eventDecls[i], @event.IpcName, info.Lazy, true, false, $"{eventDecls[i].Name}.Subscribe(value)");
            builder.Append("remove");
            AppendAnyCall(builder, eventDecls[i], @event.IpcName, info.Lazy, true, false, $"{eventDecls[i].Name}.Unsubscribe(value)");
            builder.CloseBlock().AppendLine();
        }

        i = -1;
        foreach (var method in info.Methods)
        {
            ++i;
            builder.AppendLine();
            builder.Append(
                $"{(info.DeclaringTypeKind is TypeKind.Class ? method.Accessibility.ToModifier() + "override" : "public")} {method.ReturnParameter.Type} {method.Name}({string.Join(", ", method.Parameters.Select(static param => $"{param.Type} {param.Name}"))})");
            AppendCall(builder, methodDecls[i], method.IpcName, info.Lazy, false, method.ReturnParameter, method.Parameters, string.Empty);
        }

        return SourceText.From(builder.CloseAllBlocks().ToString(), Encoding.UTF8);

        static void AppendCall(IndentedStringBuilder builder, (string TypeArguments, string Name) declaration,
            string ipcName, bool lazy, bool sameLine, ParameterInfo returnParameter,
            ValueCollection<ParameterInfo> parameters, string trailingParameters)
        {
            var parameterList = trailingParameters.Length > 0
                ? string.Join(string.Empty, parameters.Select(static parameter => $"{parameter.GetMarshalExpression(parameter.Name)}, "))
                : string.Join(", ",         parameters.Select(static parameter => parameter.GetMarshalExpression(parameter.Name)));
            var call = returnParameter.IsVoid
                ? $"{declaration.Name}.InvokeAction({parameterList}{trailingParameters})"
                : returnParameter.GetMarshalBackExpression(
                    $"{declaration.Name}.InvokeFunc({parameterList}{trailingParameters})"
                );
            AppendAnyCall(builder, declaration, ipcName, lazy, sameLine, !returnParameter.IsVoid, call);
        }

        static void AppendAnyCall(IndentedStringBuilder builder, (string TypeArguments, string Name) declaration,
            string ipcName, bool lazy, bool sameLine, bool @return, string call)
        {
            if (lazy)
            {
                builder.AppendLine();
                builder.OpenBlock();
                AppendInitialization(builder, false, declaration, ipcName, lazy, string.Empty);
                builder.AppendLine(@return ? $"return {call};" : $"{call};");
                builder.CloseBlock().AppendLine();
            }
            else
            {
                if (!sameLine)
                {
                    builder.AppendLine();
                    builder.Append("   ");
                }
                builder.AppendLine($" => {call};");
            }
        }
    }

    private static SourceText GenerateProviderPartialClass(in IpcProviderOrSubscriberInfo info)
    {
        var builder = IndentedStringBuilder.CreatePreamble()
            .OpenNamespace(info.DeclaringTypeNamespace);

        builder.AppendLine($"partial {info.DeclaringTypeKind.ToKeyword()} {info.DeclaringType.Name}");
        builder.OpenBlock();
        builder.GeneratedAttribute();
        builder.AppendLine(
            $"{info.Accessibility.ToModifier()}{(info.IsNew ? "new " : string.Empty)}static partial {info.ReturnType} {info.MethodName}(global::{IDalamudPluginInterfaceName} {info.PluginInterfaceName}, {info.ImplementationParameter.Type} {info.ImplementationParameter.Name})");
        builder.AppendLine(
            $"    => new __{info.MethodName}_ProviderImplementation({info.PluginInterfaceName}, {info.ImplementationParameter.Name});");

        var allNames = new HashSet<string>();
        allNames.Add($"__{info.MethodName}_SubscriberImplementation");
        allNames.Add("_pi");
        allNames.Add("_impl");
        allNames.Add("pi");
        allNames.Add("impl");
        allNames.Add("e");
        AddExposedMemberNames(allNames, info);
        PrepareDeclarationLists(info, out var getterDecls, out var setterDecls, out var eventDecls, out var methodDecls);

        builder.AppendLine();
        builder.AppendLine("/// <summary> This is a supporting class for a generated method and has to be exposed due to a technical limitation, but should not be used directly. </summary>");
        builder.GeneratedAttribute();
        builder.AppendLine($"private sealed class __{info.MethodName}_ProviderImplementation : {info.ReturnType}");
        builder.OpenBlock();
        builder.AppendLine($"private readonly global::{IDalamudPluginInterfaceName} _pi;");
        builder.AppendLine($"private readonly {info.ImplementationParameter.Type} _impl;");
        builder.AppendLine();
        AppendAllDeclarations(builder, info, true, allNames, getterDecls, setterDecls, eventDecls, methodDecls);
        builder.AppendLine();
        builder.AppendLine(
            $"public __{info.MethodName}_ProviderImplementation(global::{IDalamudPluginInterfaceName} pi, {info.ImplementationParameter.Type} impl)");
        builder.OpenBlock();
        builder.AppendLine("_pi = pi;");
        builder.AppendLine("_impl = impl;");
        AppendAllInitializations(builder, info, true, getterDecls, setterDecls, eventDecls, methodDecls);
        builder.CloseBlock().AppendLine();
        builder.AppendLine();
        builder.AppendLine("public void Dispose()");
        builder.OpenBlock();
        for (var i = info.Methods.Count; i-- > 0;)
        {
            builder.AppendLine($"{methodDecls[i].Name}?.Unregister{(info.Methods[i].IsAction ? "Action" : "Func")}();");
        }

        for (var i = info.Events.Count; i-- > 0;)
        {
            builder.AppendLine($"if ({eventDecls[i].Name} is not null)");
            builder.OpenBlock();
            builder.AppendLine($"_impl.{info.Events[i].Name} -= {eventDecls[i].Name}.SendMessage;");
            builder.CloseBlock().AppendLine();
        }

        for (var i = info.Properties.Count; i-- > 0;)
        {
            if (info.Properties[i].SetIpcName.Length > 0)
                builder.AppendLine($"{setterDecls[i].Name}?.UnregisterAction();");

            if (info.Properties[i].GetIpcName.Length > 0)
                builder.AppendLine($"{getterDecls[i].Name}?.UnregisterFunc();");
        }

        return SourceText.From(builder.CloseAllBlocks().ToString(), Encoding.UTF8);
    }

    private static void AddExposedMemberNames(HashSet<string> allNames, IpcProviderOrSubscriberInfo info)
    {
        foreach (var property in info.Properties)
            allNames.Add(property.Name);

        foreach (var @event in info.Events)
            allNames.Add(@event.Name);

        foreach (var method in info.Methods)
            allNames.Add(method.Name);
    }

    private static string AllocateName(string baseName, HashSet<string> allNames)
    {
        if (allNames.Add(baseName))
            return baseName;

        for (var suffix = 2;; ++suffix)
        {
            var name = $"{baseName}{suffix}";
            if (allNames.Add(name))
                return name;
        }
    }

    private static void PrepareDeclarationLists(IpcProviderOrSubscriberInfo info, out List<(string TypeArguments, string Name)> getterDecls,
        out List<(string TypeArguments, string Name)> setterDecls, out List<(string TypeArguments, string Name)> eventDecls,
        out List<(string TypeArguments, string Name)> methodDecls)
    {
        getterDecls = new(info.Properties.Count);
        setterDecls = new(info.Properties.Count);
        eventDecls  = new(info.Events.Count);
        methodDecls = new(info.Methods.Count);
    }

    private static void AppendAllDeclarations(IndentedStringBuilder builder, IpcProviderOrSubscriberInfo info, bool provider,
        HashSet<string> allNames, List<(string TypeArguments, string Name)> getterDecls, List<(string TypeArguments, string Name)> setterDecls,
        List<(string TypeArguments, string Name)> eventDecls, List<(string TypeArguments, string Name)> methodDecls)
    {
        foreach (var property in info.Properties)
        {
            if (property.GetIpcName.Length > 0)
            {
                var name = AllocateName($"_g{(property.IsIndexer ? "this" : property.Name)}", allNames);
                AppendDeclaration(builder, provider, getterDecls, info, property.Indices, property.ValueParameter.IpcType, name);
            }
            else
            {
                getterDecls.Add((string.Empty, string.Empty));
            }

            if (property.SetIpcName.Length > 0)
            {
                var name = AllocateName($"_s{(property.IsIndexer ? "this" : property.Name)}", allNames);
                AppendDeclaration(builder, provider, setterDecls, info, property.Indices, $"{property.ValueParameter.IpcType}, object?", name);
            }
            else
            {
                setterDecls.Add((string.Empty, string.Empty));
            }
        }

        foreach (var @event in info.Events)
        {
            var name = AllocateName($"_e{@event.Name}", allNames);
            AppendDeclaration(builder, provider, eventDecls, info, @event.Parameters, "object?", name);
        }

        foreach (var method in info.Methods)
        {
            var name = AllocateName($"_m{method.Name}", allNames);
            AppendDeclaration(builder,                                     provider, methodDecls, info, method.Parameters,
                method.IsAction ? "object?" : method.ReturnParameter.IpcType, name);
        }
    }

    private static void AppendDeclaration(IndentedStringBuilder builder, bool provider,
        List<(string TypeArguments, string Name)> declarations, IpcProviderOrSubscriberInfo info,
        ValueCollection<ParameterInfo> parameters, string trailingTypes, string name)
    {
        var typeArguments = IpcTypeList(parameters, true) + trailingTypes;
        builder.AppendLine(
            $"private {(info.Lazy ? string.Empty : "readonly ")}global::Dalamud.Plugin.Ipc.ICallGate{(provider ? "Provider" : "Subscriber")}<{typeArguments}>{(info.Lazy || provider ? "?" : string.Empty)} {name};"
        );
        declarations.Add((typeArguments, name));
    }

    private static void AppendAllInitializations(IndentedStringBuilder builder, IpcProviderOrSubscriberInfo info,
        bool provider, List<(string TypeArguments, string Name)> getterDecls,
        List<(string TypeArguments, string Name)> setterDecls, List<(string TypeArguments, string Name)> eventDecls,
        List<(string TypeArguments, string Name)> methodDecls)
    {
        if (info.Lazy)
            return;

        if (!provider)
            builder.AppendLine();

        var i = -1;
        foreach (var property in info.Properties)
        {
            ++i;
            var indexNames = string.Join(", ", property.Indices.Select(static param => param.Name));
            if (property.GetIpcName.Length > 0)
            {
                AppendInitialization(builder, provider, getterDecls[i], property.GetIpcName, false, !provider
                    ? string.Empty
                    : property.IsIndexer
                        ? $"{getterDecls[i].Name}.RegisterFunc(({indexNames}) => {property.ValueParameter.GetMarshalExpression($"_impl[{string.Join(", ", property.Indices.Select(static param => param.GetMarshalBackExpression(param.Name)))}]")});"
                        : $"{getterDecls[i].Name}.RegisterFunc(() => {property.ValueParameter.GetMarshalExpression($"_impl.{property.Name}")});");
            }

            if (property.SetIpcName.Length > 0)
            {
                AppendInitialization(builder, provider, setterDecls[i], property.SetIpcName, false,
                    !provider
                        ? string.Empty
                        : property.IsIndexer
                            ? $"{setterDecls[i].Name}.RegisterAction(({indexNames}, value) => _impl[{string.Join(", ", property.Indices.Select(static param => param.GetMarshalBackExpression(param.Name)))}] = {property.ValueParameter.GetMarshalBackExpression("value")});"
                            : $"{setterDecls[i].Name}.RegisterAction(value => _impl.{property.Name} = value);");
            }
        }

        i = -1;
        foreach (var @event in info.Events)
        {
            ++i;
            AppendInitialization(builder, provider, eventDecls[i], @event.IpcName, false,
                !provider ? string.Empty : $"_impl.{@event.Name} += {eventDecls[i].Name}.SendMessage;");
        }

        i = -1;
        foreach (var method in info.Methods)
        {
            ++i;
            AppendInitialization(builder, provider, methodDecls[i], method.IpcName, false,
                !provider
                    ? string.Empty
                    : method.ReturnParameter.IsTypeErased || method.Parameters.Any(static parameter => parameter.IsTypeErased)
                        ? $"{methodDecls[i].Name}.Register{(method.IsAction ? "Action" : "Func")}(({string.Join(", ", method.Parameters.Select(static param => param.Name))}) => {method.ReturnParameter.GetMarshalExpression($"_impl.{method.Name}({string.Join(", ", method.Parameters.Select(static param => param.GetMarshalBackExpression(param.Name)))})")});"
                        : $"{methodDecls[i].Name}.Register{(method.IsAction ? "Action" : "Func")}(_impl.{method.Name});");
        }
    }

    private static void AppendInitialization(IndentedStringBuilder builder, bool provider, (string TypeArguments, string Name) declaration,
        string ipcName, bool coalescing, string secondPart)
    {
        if (provider)
        {
            builder.AppendLine();
            builder.AppendLine("try");
            builder.OpenBlock();
            builder.AppendLine(
                $"{declaration.Name} {(coalescing ? "??" : string.Empty)}= _pi.GetIpcProvider<{declaration.TypeArguments}>({ipcName.ToLiteral()});");
            builder.AppendLine(secondPart);
            builder.CloseBlock().AppendLine();
            builder.AppendLine("catch (Exception e)");
            builder.OpenBlock();
            builder.AppendLine("Dispose();");
            builder.AppendLine($"throw new Exception({$"Error while registering IPC provider for {ipcName}".ToLiteral()}, e);");
            builder.CloseBlock().AppendLine();
        }
        else
        {
            builder.AppendLine(
                $"{declaration.Name} {(coalescing ? "??" : string.Empty)}= _pi.GetIpcSubscriber<{declaration.TypeArguments}>({ipcName.ToLiteral()});");
        }
    }

    private static string IpcTypeList(ValueCollection<ParameterInfo> parameters, bool withTrailingComma)
        => withTrailingComma
            ? string.Join(string.Empty, parameters.Select(static param => $"{param.IpcType}, "))
            : string.Join(", ",         parameters.Select(static param => param.IpcType));

    #endregion
}
