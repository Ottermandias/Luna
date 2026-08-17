using System.Text.Json;

namespace Luna;

/// <summary> Create a temporary object that tracks whether it has written any properties to omit an object if it is empty. </summary>
/// <param name="writer"> The JSON writer. </param>
/// <param name="objectName"> The property name for the object that may or may not be written. </param>
public ref struct TemporaryJsonObject(Utf8JsonWriter writer, ReadOnlySpan<byte> objectName) : IDisposable
{
    private readonly ReadOnlySpan<byte> _objectName    = objectName;
    private          bool               _startedObject = false;

    public void Dispose()
    {
        if (!_startedObject)
            return;

        writer.WriteEndObject();
    }

    /// <summary> Start the object if <paramref name="mark"/> is true and it has not been started. </summary>
    /// <param name="mark"> Whether to start the object. </param>
    /// <returns> The input <paramref name="mark"/> itself. </returns>
    public bool MarkUsed(bool mark)
    {
        if (mark)
            StartObject();
        return mark;
    }

    /// <summary> Write a property name and start the object beforehand if it is not started yet. </summary>
    /// <param name="property"> The name of the property to write. </param>
    public void WriteProperty(ReadOnlySpan<byte> property)
    {
        StartObject();
        writer.WritePropertyName(property);
    }

    /// <inheritdoc cref="JsonFunctions.WriteNonEmptyString"/>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public void WriteNonEmptyString(ReadOnlySpan<byte> property, string? text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        StartObject();
        writer.WriteString(property, text);
    }

    /// <inheritdoc cref="JsonFunctions.WriteIfNot(Utf8JsonWriter,ReadOnlySpan{byte},string,string,StringComparison)"/>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public void WriteIfNot(ReadOnlySpan<byte> property, string value, string nullValue,
        StringComparison comparer = StringComparison.Ordinal)
    {
        if (string.Equals(value, nullValue, comparer))
            return;

        StartObject();
        writer.WriteString(property, value);
    }

    private void StartObject()
    {
        if (_startedObject)
            return;

        _startedObject = true;
        writer.WritePropertyName(_objectName);
        writer.WriteStartObject();
    }

    /// <inheritdoc cref="JsonFunctions.WriteIfNot(Utf8JsonWriter,ReadOnlySpan{byte},bool,bool)"/>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public void WriteIfNot(ReadOnlySpan<byte> property, bool value, bool nullValue)
    {
        if (value == nullValue)
            return;

        StartObject();
        writer.WriteBoolean(property, value);
    }

    /// <inheritdoc cref="JsonFunctions.WriteIfNot(Utf8JsonWriter,ReadOnlySpan{byte},float,float)"/>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public void WriteIfNot(ReadOnlySpan<byte> property, float value, float nullValue)
    {
        if (value == nullValue)
            return;

        StartObject();
        writer.WriteNumber(property, value);
    }

    /// <inheritdoc cref="JsonFunctions.WriteIfNot(Utf8JsonWriter,ReadOnlySpan{byte},double,double)"/>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public void WriteIfNot(ReadOnlySpan<byte> property, double value, double nullValue)
    {
        if (value == nullValue)
            return;

        StartObject();
        writer.WriteNumber(property, value);
    }

    /// <inheritdoc cref="JsonFunctions.WriteEnumIfNot{T}(Utf8JsonWriter,ReadOnlySpan{byte},T,T)"/>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public void WriteEnumIfNot<T>(ReadOnlySpan<byte> property, T value, T nullValue) where T : unmanaged, Enum
    {
        if (EqualityComparer<T>.Default.Equals(value, nullValue))
            return;

        StartObject();
        writer.WriteString(property, value.StringU8);
    }

    /// <inheritdoc cref="JsonFunctions.WriteIfNot{T}(Utf8JsonWriter,ReadOnlySpan{byte},T,T,bool)"/>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public void WriteIfNot<T>(ReadOnlySpan<byte> property, T value, T nullValue, bool signed = true) where T : unmanaged, INumber<T>
    {
        if (signed)
            WriteSignedIfNot(property, value, nullValue);
        else
            WriteUnsignedIfNot(property, value, nullValue);
    }

    /// <summary> Only write an unsigned number property if the value is not equal to the specified null value. </summary>
    /// <param name="property"> The property name. It gets omitted entirely if <paramref name="value"/> equals <paramref name="nullValue"/>. </param>
    /// <param name="value"> The value. </param>
    /// <param name="nullValue"> The null value. </param>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public unsafe void WriteUnsignedIfNot<T>(ReadOnlySpan<byte> property, T value, T nullValue)
        where T : unmanaged
    {
        switch (sizeof(T))
        {
            case 1:
            {
                var v = Unsafe.As<T, byte>(ref value);
                if (v == Unsafe.As<T, byte>(ref nullValue))
                    return;

                StartObject();
                writer.WriteNumber(property, v);
                break;
            }
            case 2:
            {
                var v = Unsafe.As<T, ushort>(ref value);
                if (v == Unsafe.As<T, ushort>(ref nullValue))
                    return;

                StartObject();
                writer.WriteNumber(property, v);
                break;
            }
            case 4:
            {
                var v = Unsafe.As<T, uint>(ref value);
                if (v == Unsafe.As<T, uint>(ref nullValue))
                    return;

                StartObject();
                writer.WriteNumber(property, v);
                break;
            }
            case 8:
            {
                var v = Unsafe.As<T, ulong>(ref value);
                if (v == Unsafe.As<T, ulong>(ref nullValue))
                    return;

                StartObject();
                writer.WriteNumber(property, v);
                break;
            }
            default: throw new ArgumentException($"The type {typeof(T)} is not supported for {nameof(WriteUnsignedIfNot)}.");
        }
    }

    /// <summary> Only write a signed number property if the value is not equal to the specified null value. </summary>
    /// <param name="property"> The property name. It gets omitted entirely if <paramref name="value"/> equals <paramref name="nullValue"/>. </param>
    /// <param name="value"> The value. </param>
    /// <param name="nullValue"> The null value. </param>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public unsafe void WriteSignedIfNot<T>(ReadOnlySpan<byte> property, T value, T nullValue) where T : unmanaged
    {
        switch (sizeof(T))
        {
            case 1:
            {
                var v = Unsafe.As<T, sbyte>(ref value);
                if (v == Unsafe.As<T, sbyte>(ref nullValue))
                    return;

                StartObject();
                writer.WriteNumber(property, v);
                break;
            }
            case 2:
            {
                var v = Unsafe.As<T, short>(ref value);
                if (v == Unsafe.As<T, short>(ref nullValue))
                    return;

                StartObject();
                writer.WriteNumber(property, v);
                break;
            }
            case 4:
            {
                var v = Unsafe.As<T, int>(ref value);
                if (v == Unsafe.As<T, int>(ref nullValue))
                    return;

                StartObject();
                writer.WriteNumber(property, v);
                break;
            }
            case 8:
            {
                var v = Unsafe.As<T, long>(ref value);
                if (v == Unsafe.As<T, long>(ref nullValue))
                    return;

                StartObject();
                writer.WriteNumber(property, v);
                break;
            }
            default: throw new ArgumentException($"The type {typeof(T)} is not supported for {nameof(WriteSignedIfNot)}.");
        }
    }
}
