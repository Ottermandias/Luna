namespace Luna;

/// <summary> Extensions for built-in arrays and the <see cref="Array"/> type. </summary>
public static class ArrayExtensions
{
    extension(Array)
    {
        /// <summary> Create an array filled with shallow copies of an existing element. </summary>
        /// <typeparam name="T"> The element type. </typeparam>
        /// <param name="value"> The existing value. If this is a reference type, no new instances of it are created. </param>
        /// <param name="count"> The number of copies.</param>
        /// <returns> A filled array. </returns>
        [MethodImpl(ImSharpConfiguration.OptInl)]
        public static T[] Repeat<T>(in T value, int count)
        {
            var ret = new T[count];
            for (var i = 0; i < count; ++i)
                ret[i] = value;
            return ret;
        }
    }

    extension<T>(T[] array)
    {
        /// <summary> Create a new array from an existing one while leaving one entry out. </summary>
        /// <param name="index"> The index of the entry to leave out. </param>
        /// <returns> A shallow copy of the existing array with the entry left out. </returns>
        [MethodImpl(ImSharpConfiguration.OptInl)]
        public T[] Without(int index)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)array.Length, nameof(index));
            if (array.Length is 1)
                return [];

            var ret = new T[array.Length - 1];
            array.AsSpan(0, index).CopyTo(ret);
            array.AsSpan(index + 1).CopyTo(ret.AsSpan(index));
            return ret;
        }
    }
}
