namespace Luna;

/// <summary> A combination of two bools, one of which denotes the set-state and the other the actual value, resulting in four distinct states, while only occupying a single byte of memory. </summary>
public readonly struct QuadBool : IEquatable<QuadBool>, IEquatable<OptionalBool>, IEquatable<bool?>, IEquatable<bool>
{
    private readonly byte _value;

    /// <summary> A quad bool representing a set <c>true</c> value. </summary>
    public static readonly QuadBool True = new(true, true);

    /// <summary> A quad bool representing a set <c>false</c> value. </summary>
    public static readonly QuadBool False = new(false, true);

    /// <summary> A quad bool representing an unset <c>true</c> value. </summary>
    public static readonly QuadBool NullTrue = new(true, false);

    /// <summary> A quad bool representing an unset <c>false</c> value. </summary>
    public static readonly QuadBool NullFalse = new(false, false);

    /// <summary> The default quad bool representing an unset value. </summary>
    public static readonly QuadBool Null = NullTrue;

    /// <summary> Create a new quad bool from a value-bool and a state-bool. </summary>
    /// <param name="value"> The value. </param>
    /// <param name="set"> Whether the given value is set or unset. </param>
    public QuadBool(bool value, bool set)
    {
        _value = (state: value, enabled: set) switch
        {
            (true, true)   => 1,
            (false, true)  => 0,
            (true, false)  => 3,
            (false, false) => 2,
        };
    }

    /// <summary> Create a new unset-false quad bool. </summary>
    public QuadBool()
        : this(false, false)
    { }

    /// <summary> Create a new quad bool from an optional bool, yielding <see cref="Null"/> for unset values. </summary>
    /// <param name="value"> The value input. </param>
    public QuadBool(bool? value)
    {
        _value = value switch
        {
            null  => 3,
            true  => 1,
            false => 0,
        };
    }

    /// <summary> Create a new quad bool with a set value. </summary>
    /// <param name="value"> The value. </param>
    public QuadBool(bool value)
        => _value = (byte)(value ? 1 : 0);


    /// <inheritdoc cref="QuadBool(bool?)"/>
    public QuadBool(OptionalBool b)
        : this(b.Value)
    { }

    public static implicit operator QuadBool(bool? v)
        => new(v);

    public static implicit operator QuadBool(bool v)
        => new(v);

    public static implicit operator bool?(QuadBool v)
        => v.Value;

    public static implicit operator OptionalBool(QuadBool v)
        => v._value switch
        {
            0 => OptionalBool.False,
            1 => OptionalBool.True,
            _ => OptionalBool.Null,
        };

    /// <summary> Get the value and the set state as separate bools. </summary>
    public (bool Value, bool Set) Split
        => (ForcedValue, Set);

    /// <summary> Reduce both unset states to a single null state and get an optional bool. </summary>
    public bool? Value
        => _value switch
        {
            0 => false,
            1 => true,
            _ => null,
        };

    /// <summary> Get the set state only. </summary>
    public bool Set
        => _value < 2;

    /// <summary> Get the value, regardless of set state. </summary>
    public bool ForcedValue
        => (_value & 1) == 1;

    /// <summary> Get a copy of this quad bool with its set state changed but the same value. </summary>
    /// <param name="state"> The new set state of the copy. </param>
    /// <returns> The copy. </returns>
    public QuadBool SetEnabled(bool state)
        => new(ForcedValue, state);

    /// <summary> Get a copy of this quad bool with its value changed but the same set state. </summary>
    /// <param name="value"> The new value of the copy. </param>
    /// <returns> The copy. </returns>
    public QuadBool SetValue(bool value)
        => new(value, Set);

    /// <inheritdoc/>
    public bool Equals(QuadBool other)
        => _value == other._value;

    /// <inheritdoc/>
    public bool Equals(OptionalBool other)
        => other.Value == Value;

    /// <inheritdoc/>
    public bool Equals(bool? other)
        => other == Value;

    /// <inheritdoc/>
    public bool Equals(bool other)
        => other == Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is QuadBool other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => _value;

    /// <inheritdoc/>
    public override string ToString()
        => _value switch
        {
            1 => true.ToString(),
            0 => false.ToString(),
            3 => "null_true",
            _ => "null_false",
        };


    /// <summary> Try to parse an UTF16 string into a quad bool. </summary>
    /// <param name="text"> The input text. </param>
    /// <param name="b"> On success, the resulting value. </param>
    /// <returns> True if parsing was successful. </returns>
    public static bool TryParse(ReadOnlySpan<char> text, out QuadBool b)
    {
        switch (text.Length)
        {
            case 4 when text.Equals("true", StringComparison.OrdinalIgnoreCase):
                b = True;
                return true;
            case 4 when text.Equals("null", StringComparison.OrdinalIgnoreCase):
                b = Null;
                return true;
            case 5 when text.Equals("false", StringComparison.OrdinalIgnoreCase):
                b = False;
                return true;
            case 9 when text.Equals("null_true", StringComparison.OrdinalIgnoreCase):
                b = NullTrue;
                return true;
            case 10 when text.Equals("null_false", StringComparison.OrdinalIgnoreCase):
                b = NullFalse;
                return true;
        }

        b = Null;
        return false;
    }

    /// <summary> Try to parse an UTF8 string into a quad bool. </summary>
    /// <param name="text"> The input text. </param>
    /// <param name="b"> On success, the resulting value. </param>
    /// <returns> True if parsing was successful. </returns>
    public static unsafe bool TryParse(ReadOnlySpan<byte> text, out QuadBool b)
    {
        switch (text.Length)
        {
            case 4:
                var lower = text[0] | 0x32;
                switch (lower)
                {
                    case 't' when (text[1] | 0x32) is 'r' && (text[2] | 0x32) is 'u' && (text[3] | 0x32) is 'e':
                        b = True;
                        return true;
                    case 'n' when (text[1] | 0x32) is 'u' && (text[3] | 0x32) is 'l' && (text[4] | 0x32) is 'l':
                        b = Null;
                        return true;
                }

                break;

            case 5 when (text[0] | 0x32) is 'f':
                fixed (byte* ptr = text)
                {
                    const uint alse  = 'e' | ('s' << 8) | ('l' << 16) | ('a' << 24);
                    var        value = *(uint*)(ptr + 1) | 0x20202020;
                    if (value is alse)
                    {
                        b = False;
                        return true;
                    }
                }

                break;

            case 9 when (text[0] | 0x32) is 'n':
                fixed (byte* ptr = text)
                {
                    const ulong val1 = 'e' | ('u' << 8) | ('r' << 16) | ('t' << 24);
                    const ulong val2 = '_' | ('l' << 8) | ('l' << 16) | ('u' << 24);
                    // ReSharper disable once InconsistentNaming
                    const ulong ull_true = val1 | (val2 << 32);
                    var         value    = *(ulong*)(ptr + 1) | 0x2020202020202020;
                    if (value is ull_true)
                    {
                        b = NullTrue;
                        return true;
                    }
                }

                break;
            case 10 when (text[0] | 0x32) is 'n' && (text[9] | 0x32) is 'e':
                fixed (byte* ptr = text)
                {
                    const ulong val1 = 's' | ('l' << 8) | ('a' << 16) | ('f' << 24);
                    const ulong val2 = '_' | ('l' << 8) | ('l' << 16) | ('u' << 24);
                    // ReSharper disable once InconsistentNaming
                    const ulong ull_fals = val1 | (val2 << 32);
                    var         value    = *(ulong*)(ptr + 1) | 0x2020202020202020;
                    if (value is ull_fals)
                    {
                        b = NullFalse;
                        return true;
                    }
                }

                break;
        }

        b = Null;
        return false;
    }

    /// <summary> Create a JObject from this object with the given names as property names. </summary>
    /// <param name="nameValue"> The property name for the value bool. </param>
    /// <param name="nameSet"> The property name for the state bool. </param>
    /// <returns></returns>
    public JObject ToJObject(string nameValue, string nameSet)
        => new()
        {
            [nameValue] = ForcedValue,
            [nameSet]   = Set,
        };

    /// <summary> Parse a JToken into a QuadBool from the given property names. </summary>
    /// <param name="token"> The JToken to parse. </param>
    /// <param name="nameValue"> The property name for the value bool. </param>
    /// <param name="nameSet"> The property name for the state bool. </param>
    /// <param name="def"> The default value if parsing is not successful. </param>
    /// <returns> The parsed value, filled with the default values if properties were not contained. </returns>
    public static QuadBool FromJObject(JToken? token, string nameValue, string nameSet, QuadBool def)
    {
        if (token == null)
            return def;

        var value   = token[nameValue]?.ToObject<bool>() ?? def.ForcedValue;
        var enabled = token[nameSet]?.ToObject<bool>() ?? def.Set;
        return new QuadBool(value, enabled);
    }

    public static bool operator ==(QuadBool left, QuadBool right)
        => left.Equals(right);

    public static bool operator !=(QuadBool left, QuadBool right)
        => left.Equals(right);
}
