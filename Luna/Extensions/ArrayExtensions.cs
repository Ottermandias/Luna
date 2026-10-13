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
}
