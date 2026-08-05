using System.Text.Json;

namespace Luna;

public static partial class JsonFunctions
{
    /// <param name="j"> The JSON writer. </param>
    extension(Utf8JsonWriter j)
    {
        /// <summary> Only write a string property if the string is neither null nor empty. </summary>
        /// <param name="property"> The property name. It gets omitted entirely if <paramref name="text"/> is null or empty. </param>
        /// <param name="text"> The text value. </param>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public void WriteNonEmptyString(ReadOnlySpan<byte> property, string? text)
        {
            if (!string.IsNullOrEmpty(text))
                j.WriteString(property, text);
        }

        /// <summary> Only write a string property if the string is not equal to the specified null value. </summary>
        /// <param name="property"> The property name. It gets omitted entirely if <paramref name="value"/> equals <paramref name="nullValue"/>. </param>
        /// <param name="value"> The text value. </param>
        /// <param name="nullValue"> The null value. </param>
        /// <param name="comparer"> The comparer to use. </param>
        [MethodImpl(ImSharpConfiguration.Inl)]
        public void WriteIfNot(ReadOnlySpan<byte> property, string value, string nullValue,
            StringComparison comparer = StringComparison.Ordinal)
        {
            if (!string.Equals(value, nullValue, comparer))
                j.WriteString(property, value);
        }

        /// <summary> Only write a boolean property if the value is not equal to the specified null value. </summary>
        /// <param name="property"> The property name. It gets omitted entirely if <paramref name="value"/> equals <paramref name="nullValue"/>. </param>
        /// <param name="value"> The value. </param>
        /// <param name="nullValue"> The null value. </param>
        [MethodImpl(ImSharpConfiguration.Inl)]
        [OverloadResolutionPriority(100)]
        public void WriteIfNot(ReadOnlySpan<byte> property, bool value, bool nullValue)
        {
            if (value != nullValue)
                j.WriteBoolean(property, value);
        }

        /// <summary> Only write a number property if the value is not equal to the specified null value. </summary>
        /// <param name="property"> The property name. It gets omitted entirely if <paramref name="value"/> equals <paramref name="nullValue"/>. </param>
        /// <param name="value"> The value. </param>
        /// <param name="nullValue"> The null value. </param>
        [MethodImpl(ImSharpConfiguration.Inl)]
        [OverloadResolutionPriority(100)]
        public void WriteIfNot(ReadOnlySpan<byte> property, float value, float nullValue)
        {
            if (nullValue != value)
                j.WriteNumber(property, value);
        }

        /// <inheritdoc cref="WriteIfNot(Utf8JsonWriter,ReadOnlySpan{byte},float,float)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        [OverloadResolutionPriority(100)]
        public void WriteIfNot(ReadOnlySpan<byte> property, double value, double nullValue)
        {
            if (nullValue != value)
                j.WriteNumber(property, value);
        }

        /// <summary> Only write an enum property as string if the value is not equal to the specified null value. </summary>
        /// <param name="property"> The property name. It gets omitted entirely if <paramref name="value"/> equals <paramref name="nullValue"/>. </param>
        /// <param name="value"> The value. </param>
        /// <param name="nullValue"> The null value. </param>
        [MethodImpl(ImSharpConfiguration.Inl)]
        [OverloadResolutionPriority(100)]
        public void WriteEnumIfNot<T>(ReadOnlySpan<byte> property, T value, T nullValue) where T : unmanaged, Enum
        {
            if (!EqualityComparer<T>.Default.Equals(value, nullValue))
                j.WriteString(property, value.StringU8);
        }

        /// <inheritdoc cref="WriteIfNot(Utf8JsonWriter,ReadOnlySpan{byte},float,float)"/>
        [MethodImpl(ImSharpConfiguration.Inl)]
        [OverloadResolutionPriority(0)]
        public void WriteIfNot<T>(ReadOnlySpan<byte> property, T value, T nullValue, bool signed = true) where T : unmanaged, INumber<T>
        {
            if (signed)
                j.WriteSignedIfNot(property, value, nullValue);
            else
                j.WriteUnsignedIfNot(property, value, nullValue);
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
                    if (v != Unsafe.As<T, byte>(ref nullValue))
                        j.WriteNumber(property, v);
                    break;
                }
                case 2:
                {
                    var v = Unsafe.As<T, ushort>(ref value);
                    if (v != Unsafe.As<T, ushort>(ref nullValue))
                        j.WriteNumber(property, v);
                    break;
                }
                case 4:
                {
                    var v = Unsafe.As<T, uint>(ref value);
                    if (v != Unsafe.As<T, uint>(ref nullValue))
                        j.WriteNumber(property, v);
                    break;
                }
                case 8:
                {
                    var v = Unsafe.As<T, ulong>(ref value);
                    if (v != Unsafe.As<T, ulong>(ref nullValue))
                        j.WriteNumber(property, v);
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
                    if (v != Unsafe.As<T, sbyte>(ref nullValue))
                        j.WriteNumber(property, v);
                    break;
                }
                case 2:
                {
                    var v = Unsafe.As<T, short>(ref value);
                    if (v != Unsafe.As<T, short>(ref nullValue))
                        j.WriteNumber(property, v);
                    break;
                }
                case 4:
                {
                    var v = Unsafe.As<T, int>(ref value);
                    if (v != Unsafe.As<T, int>(ref nullValue))
                        j.WriteNumber(property, v);
                    break;
                }
                case 8:
                {
                    var v = Unsafe.As<T, long>(ref value);
                    if (v != Unsafe.As<T, long>(ref nullValue))
                        j.WriteNumber(property, v);
                    break;
                }
                default: throw new ArgumentException($"The type {typeof(T)} is not supported for {nameof(WriteSignedIfNot)}.");
            }
        }
    }
}
