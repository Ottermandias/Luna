//HintName: StrongType.Test.g.cs
using System;
using System.CodeDom.Compiler;

#nullable enable

namespace Luna.Test
{
    public readonly partial struct Test(uint Value)
        : IParsable<Test>,
          ISpanParsable<Test>,
          IUtf8SpanParsable<Test>,
          IFormattable,
          ISpanFormattable,
          IUtf8SpanFormattable,
          global::System.IEquatable<Test>,
          global::System.Numerics.IEqualityOperators<Test, Test, bool>,
          global::System.IEquatable<uint>,
          global::System.Numerics.IEqualityOperators<Test, uint, bool>,
          global::System.IComparable<Test>,
          global::System.Numerics.IComparisonOperators<Test, Test, bool>,
          global::System.IComparable<uint>,
          global::System.Numerics.IComparisonOperators<Test, uint, bool>,
          global::System.Numerics.IIncrementOperators<Test>,
          global::System.Numerics.IDecrementOperators<Test>,
          global::System.Numerics.IAdditionOperators<Test, Test, Test>,
          global::System.Numerics.IAdditiveIdentity<Test, Test>,
          global::System.Numerics.IAdditionOperators<Test, uint, Test>,
          global::System.Numerics.IAdditiveIdentity<Test, uint>,
          global::System.Numerics.ISubtractionOperators<Test, Test, Test>,
          global::System.Numerics.ISubtractionOperators<Test, uint, Test>
    {
        public readonly uint Value = Value;
        
        /// <inheritdoc/>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public override string ToString()
            => Value.ToString();
        
        /// <inheritdoc/>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static Test Parse(string s, IFormatProvider? provider)
            => uint.Parse(s, provider);
        
        /// <inheritdoc/>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool TryParse(string? s, IFormatProvider? provider, out Test result)
        {
            if (uint.TryParse(s, provider, out var v))
            {
                result = new Test(v);
                return true;
            }
            result = default;
            return false;
        }
        
        /// <inheritdoc/>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static Test Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
            => uint.Parse(s, provider);
        
        /// <inheritdoc/>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Test result)
        {
            if (uint.TryParse(s, provider, out var v))
            {
                result = new Test(v);
                return true;
            }
            result = default;
            return false;
        }
        
        /// <inheritdoc/>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static Test Parse(ReadOnlySpan<byte> s, IFormatProvider? provider)
            => uint.Parse(s, provider);
        
        /// <inheritdoc/>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool TryParse(ReadOnlySpan<byte> s, IFormatProvider? provider, out Test result)
        {
            if (uint.TryParse(s, provider, out var v))
            {
                result = new Test(v);
                return true;
            }
            result = default;
            return false;
        }
        
        /// <inheritdoc/>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public string ToString(string? format, IFormatProvider?  formatProvider)
            => Value.ToString(format, formatProvider);
        
        /// <inheritdoc/>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? formatProvider)
            => Value.TryFormat(destination, out charsWritten, format, formatProvider);
        
        /// <inheritdoc/>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? formatProvider)
            => Value.TryFormat(destination, out bytesWritten, format, formatProvider);
        
        /// <inheritdoc/>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public bool Equals(Test other)
            => Value.Equals(other.Value);
        
        /// <inheritdoc/>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public override bool Equals(object? obj)
            => (obj is Test other && other.Value.Equals(Value))
            || (obj is uint baseType && baseType.Equals(Value));
        
        /// <inheritdoc/>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public override int GetHashCode()
            => Value.GetHashCode();
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool operator ==(Test lhs, Test rhs)
            => lhs.Value == rhs.Value;
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool operator !=(Test lhs, Test rhs)
            => lhs.Value != rhs.Value;
        
        /// <inheritdoc/>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public bool Equals(uint other)
            => Value.Equals(other);
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool operator ==(Test lhs, uint rhs)
            => lhs.Value == rhs;
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool operator !=(Test lhs, uint rhs)
            => lhs.Value != rhs;
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool operator ==(uint lhs, Test rhs)
            => lhs == rhs.Value;
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool operator !=(uint lhs, Test rhs)
            => lhs != rhs.Value;
        
        /// <inheritdoc/>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public int CompareTo(Test other)
            => Value.CompareTo(other.Value);
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool operator >(Test lhs, Test rhs)
            => lhs.Value > rhs.Value;
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool operator <(Test lhs, Test rhs)
            => lhs.Value < rhs.Value;
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool operator >=(Test lhs, Test rhs)
            => lhs.Value >= rhs.Value;
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool operator <=(Test lhs, Test rhs)
            => lhs.Value <= rhs.Value;
        
        /// <inheritdoc/>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public int CompareTo(uint other)
            => Value.CompareTo(other);
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool operator >(Test lhs, uint rhs)
            => lhs.Value > rhs;
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool operator <(Test lhs, uint rhs)
            => lhs.Value < rhs;
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool operator >=(Test lhs, uint rhs)
            => lhs.Value >= rhs;
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool operator <=(Test lhs, uint rhs)
            => lhs.Value <= rhs;
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool operator >(uint lhs, Test rhs)
            => lhs > rhs.Value;
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool operator <(uint lhs, Test rhs)
            => lhs < rhs.Value;
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool operator >=(uint lhs, Test rhs)
            => lhs >= rhs.Value;
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static bool operator <=(uint lhs, Test rhs)
            => lhs <= rhs.Value;
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static implicit operator Test(uint value)
            => new(value);
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static implicit operator uint(Test value)
            => value.Value;
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static Test operator ++(Test value)
        {
            var v = value.Value;
            return new(++v);
        }
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static Test operator --(Test value)
        {
            var v = value.Value;
            return new(--v);
        }
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static Test operator +(Test lhs, Test rhs)
            => new(lhs.Value + rhs.Value);
        
        /// <inheritdoc/>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static Test AdditiveIdentity
            => default;
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static Test operator +(Test lhs, uint rhs)
            => new(lhs.Value + rhs);
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static Test operator +(uint lhs, Test rhs)
            => new(lhs + rhs.Value);
        
        /// <inheritdoc/>
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        static uint global::System.Numerics.IAdditiveIdentity<Test, uint>.AdditiveIdentity
            => default;
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static Test operator -(Test lhs, Test rhs)
            => new(lhs.Value - rhs.Value);
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static Test operator -(Test lhs, uint rhs)
            => new(lhs.Value - rhs);
        
        [GeneratedCode("Luna.Generators", "1.0.0.0")]
        public static Test operator -(uint lhs, Test rhs)
            => new(lhs - rhs.Value);
        
    }
}

