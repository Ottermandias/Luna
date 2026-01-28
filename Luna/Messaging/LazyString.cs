namespace Luna;

/// <summary> A lazily evaluated string. </summary>
/// <remarks> The generator will at most be invoked once. </remarks>
public sealed class LazyString
{
    private readonly Func<string>? _generator;
    private          string?       _value;

    /// <summary> A lazily evaluated string. </summary>
    /// <param name="generator"> The generator to produce the string. </param>
    public LazyString(Func<string> generator)
        => _generator = generator;

    /// <summary> Create an already evaluated lazy string. </summary>
    /// <param name="text"> The text to return. </param>
    public LazyString(string text = "")
        => _value = text;

    /// <summary> Whether the lazy string has been evaluated already. </summary>
    public bool IsEvaluated
        => _value is not null;

    /// <summary> Get the actual string, invoking the generator once if <see cref="IsEvaluated"/> is still false. </summary>
    public string Value
        => _value ??= _generator!(); // The generator can not be null since the value is set by the constructor in that case.

    /// <inheritdoc/>
    public override string ToString()
        => Value;
}
