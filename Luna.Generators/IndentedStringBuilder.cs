using System.Reflection;
using System.Text;

namespace Luna.Generators;

public class IndentedStringBuilder
{
    private readonly StringBuilder _sb          = new();
    private          string        _indentation = string.Empty;

    public IndentedStringBuilder Indent()
    {
        _indentation = $"{_indentation}    ";
        return this;
    }

    public IndentedStringBuilder Unindent()
    {
        _indentation =  _indentation.Substring(4);
        _sb.Length   -= 4;
        return this;
    }

    public IndentedStringBuilder AppendLine()
    {
        _sb.AppendLine().Append(_indentation);
        return this;
    }

    public IndentedStringBuilder AppendLine(string text)
    {
        _sb.AppendLine(text).Append(_indentation);
        return this;
    }

    public IndentedStringBuilder Append(string text)
    {
        _sb.Append(text);
        return this;
    }

    public IndentedStringBuilder Append(char symbol)
    {
        _sb.Append(symbol);
        return this;
    }

    public IndentedStringBuilder OpenBlock()
        => Append('{').Indent().AppendLine();

    public IndentedStringBuilder CloseBlock()
        => Unindent().Append('}');

    public static IndentedStringBuilder CreatePreamble()
        => new IndentedStringBuilder().AppendLine("using System;").AppendLine("using System.CodeDom.Compiler;").AppendLine().AppendLine("#nullable enable").AppendLine();

    public IndentedStringBuilder OpenNamespace(string @namespace)
    {
        if (@namespace.Length is 0)
            return this;

        return Append("namespace ").Append(@namespace).AppendLine().OpenBlock();
    }

    public IndentedStringBuilder CloseAllBlocks()
    {
        while (_indentation.Length > 0)
            CloseBlock().AppendLine();
        return AppendLine();
    }

    public override string ToString()
        => _sb.ToString();

    public IndentedStringBuilder OpenExtensionClass(string @class)
    {
        var dot = @class.LastIndexOf('.');
        if (dot >= 0)
            @class = @class.Substring(dot + 1);
        return Append("public static partial class ").Append(@class).AppendLine().OpenBlock();
    }

    public IndentedStringBuilder AppendObject(string name, string @namespace = "")
    {
        Append("global::");
        if (@namespace.Length > 0)
            Append(@namespace).Append('.');
        return Append(name);
    }

    public IndentedStringBuilder GeneratedAttribute()
    {
        var assemblyName = Assembly.GetAssembly(typeof(IndentedStringBuilder)).GetName();
        return AppendLine($"[GeneratedCode(\"{assemblyName.Name}\", \"{assemblyName.Version}\")]");
    }
}
