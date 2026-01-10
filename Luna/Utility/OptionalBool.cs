namespace Luna;

/// <summary> An optimized optional bool that still only occupies a single byte of memory. </summary>
public readonly struct OptionalBool : IEquatable<OptionalBool>, IEquatable<bool?>, IEquatable<bool>
{
    private readonly byte _value;

    /// <summary> An optional bool representing the <c>true</c> value. </summary>
    public static readonly OptionalBool True  = new(true);

    /// <summary> An optional bool representing the <c>false</c> value. </summary>
    public static readonly OptionalBool False = new(false);

    /// <summary> An optional bool representing the <c>null</c>, or unset, value. </summary>
    public static readonly OptionalBool Null  = new();

    /// <summary> Create an optional bool in unset state. </summary>
    public OptionalBool()
        => _value = byte.MaxValue;

    /// <summary> Create an optional bool from a nullable bool. </summary>
    public OptionalBool(bool? value)
        => _value = (byte)(value is null ? byte.MaxValue : value.Value ? 1 : 0);

    /// <summary> Whether the value is set to either <c>true</c> or <c>false</c>. </summary>
    public bool HasValue
        => _value < 2;

    /// <summary> Get the value as a <see cref="Nullable{Boolean}"/>. </summary>
    public bool? Value
        => _value switch
        {
            1 => true,
            0 => false,
            _ => null,
        };

    public static implicit operator OptionalBool(bool? v)
        => new(v);

    public static implicit operator OptionalBool(bool v)
        => new(v);

    public static implicit operator bool?(OptionalBool v)
        => v.Value;

    /// <inheritdoc/>
    public bool Equals(OptionalBool other)
        => _value == other._value;

    /// <summary> Get whether the value is set to true. </summary>
    public bool IsTrue
        => _value is 1;

    /// <summary> Get whether the value is set to false. </summary>
    public bool IsFalse
        => _value is 0;

    /// <summary> Get whether the value is not set to true or false. </summary>
    public bool IsNull
        => _value > 1;

    /// <inheritdoc/>
    public bool Equals(bool? other)
        => _value switch
        {
            1 when other != null => other.Value,
            0 when other != null => !other.Value,
            _ when other == null => true,
            _                    => false,
        };

    /// <inheritdoc/>
    public bool Equals(bool other)
        => other ? _value == 1 : _value == 0;

    /// <inheritdoc/>
    public override string ToString()
        => _value switch
        {
            1 => true.ToString(),
            0 => false.ToString(),
            _ => "null",
        };
}
