using Microsoft.CodeAnalysis;

namespace Luna.Generators;

internal readonly record struct IpcProviderOrSubscriberInfo
{
    public readonly TypeDefinition                DeclaringType;
    public readonly TypeKind                      DeclaringTypeKind;
    public readonly string                        DeclaringTypeNamespace;
    public readonly string                        MethodName;
    public readonly string                        ReturnType;
    public readonly Accessibility                 Accessibility;
    public readonly bool                          IsNew;
    public readonly bool                          Lazy;
    public readonly string                        PluginInterfaceName;
    public readonly ParameterInfo                 ImplementationParameter;
    public readonly ValueCollection<MethodInfo>   Methods;
    public readonly ValueCollection<PropertyInfo> Properties;
    public readonly ValueCollection<EventInfo>    Events;

    public IpcProviderOrSubscriberInfo(string declaringType, TypeKind declaringTypeKind, string declaringTypeNamespace,
        string methodName, string returnType, Accessibility accessibility, bool isNew, bool lazy, string pluginInterfaceName,
        ParameterInfo implementationParameter, IReadOnlyList<MethodInfo> methods, IReadOnlyList<PropertyInfo> properties,
        IReadOnlyList<EventInfo> events)
    {
        DeclaringType           = new(declaringType);
        DeclaringTypeKind       = declaringTypeKind;
        DeclaringTypeNamespace  = declaringTypeNamespace;
        MethodName              = methodName;
        ReturnType              = returnType;
        Accessibility           = accessibility;
        IsNew                   = isNew;
        Lazy                    = lazy;
        PluginInterfaceName     = pluginInterfaceName;
        ImplementationParameter = implementationParameter;
        Methods                 = new(methods);
        Properties              = new(properties);
        Events                  = new(events);
    }
}

internal readonly record struct MethodInfo
{
    public readonly string                         Name;
    public readonly ParameterInfo                  ReturnParameter;
    public readonly string                         IpcName;
    public readonly Accessibility                  Accessibility;
    public readonly ValueCollection<ParameterInfo> Parameters;

    public bool IsAction
        => ReturnParameter.IsVoid;

    public MethodInfo(string name, ParameterInfo returnParameter, string ipcName, Accessibility accessibility,
        params IReadOnlyList<ParameterInfo> parameters)
    {
        Name            = name;
        ReturnParameter = returnParameter;
        IpcName         = ipcName;
        Accessibility   = accessibility;
        Parameters      = new(parameters);
    }
}

internal readonly record struct PropertyInfo
{
    public readonly string                         Name;
    public readonly ParameterInfo                  ValueParameter;
    public readonly string                         GetIpcName;
    public readonly string                         SetIpcName;
    public readonly Accessibility                  Accessibility;
    public readonly Accessibility                  GetAccessibility;
    public readonly Accessibility                  SetAccessibility;
    public readonly ValueCollection<ParameterInfo> Indices;

    public bool IsIndexer
        => Name is "this[]";

    public PropertyInfo(string name, ParameterInfo valueParameter, string getIpcName, string setIpcName, Accessibility accessibility,
        Accessibility getAccessibility, Accessibility setAccessibility, params IReadOnlyList<ParameterInfo> indices)
    {
        Name             = name;
        ValueParameter   = valueParameter;
        GetIpcName       = getIpcName;
        SetIpcName       = setIpcName;
        Accessibility    = accessibility;
        GetAccessibility = getAccessibility;
        SetAccessibility = setAccessibility;
        Indices          = new(indices);
    }
}

internal readonly record struct EventInfo
{
    public readonly string                         Name;
    public readonly string                         Type;
    public readonly string                         IpcName;
    public readonly Accessibility                  Accessibility;
    public readonly ValueCollection<ParameterInfo> Parameters;

    public EventInfo(string name, string type, string ipcName, Accessibility accessibility, params IReadOnlyList<ParameterInfo> parameters)
    {
        Name          = name;
        Type          = type;
        IpcName       = ipcName;
        Accessibility = accessibility;
        Parameters    = new(parameters);
    }
}

internal readonly record struct ParameterInfo
{
    public static readonly ParameterInfo Void = new(string.Empty, "void");

    public readonly string Name;
    public readonly string Type;
    public readonly string IpcType;
    public readonly string Marshal;
    public readonly string MarshalBack;

    public bool IsVoid
        => Type is "void";

    public bool IsTypeErased
        => !IpcType.Equals(Type, StringComparison.Ordinal);

    private ParameterInfo(string name, string type)
    {
        Name        = name;
        Type        = type;
        IpcType     = type;
        Marshal     = string.Empty;
        MarshalBack = string.Empty;
    }

    public ParameterInfo(string name, ITypeSymbol type, ITypeSymbol ipcType, string marshal, string marshalBack)
    {
        Name        = name;
        Type        = type.ToString();
        IpcType     = ipcType.ToString();
        Marshal     = marshal;
        MarshalBack = marshalBack;
    }

    public ParameterInfo(string name, ITypeSymbol type, string ipcType, string marshal, string marshalBack)
    {
        Name        = name;
        Type        = type.ToString();
        IpcType     = ipcType;
        Marshal     = marshal;
        MarshalBack = marshalBack;
    }

    public ParameterInfo(string name, ITypeSymbol type) : this(name, type, type, string.Empty, string.Empty)
    {
    }

    public string GetMarshalExpression(string valueExpression)
    {
        if (Marshal.Length > 0)
            return GetCustomMarshalExpression(Type, Marshal, valueExpression);

        if (MarshalBack.Length > 0 && MarshalBack is not "new" || !IsTypeErased || IpcType is "object" or "object?")
            return valueExpression;

        return $"({IpcType}){valueExpression}";
    }

    public string GetMarshalBackExpression(string valueExpression)
    {
        if (MarshalBack.Length > 0)
        {
            return MarshalBack is "new"
                ? $"new {Type}({valueExpression})"
                : GetCustomMarshalExpression(Type, MarshalBack, valueExpression);
        }

        if (Marshal.Length > 0 || !IsTypeErased)
            return valueExpression;

        return $"({Type}){valueExpression}";
    }

    private static string GetCustomMarshalExpression(string type, string marshal, string valueExpression)
    {
        if (marshal.StartsWith(".", StringComparison.Ordinal))
            return $"{valueExpression}{marshal}";

        if (marshal.Contains(".", StringComparison.Ordinal))
            return $"{marshal}({valueExpression})";

        return $"{type}.{marshal}({valueExpression})";
    }
}
