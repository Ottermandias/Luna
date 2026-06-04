namespace Luna;

/// <summary> Extensions for <see cref="List{T}"/> manipulation and utility. </summary>
public static class ListExtensions
{
    /// <summary> Add an object to a list if it does not exist yet, or replace the contained object comparing equal to it. </summary>
    /// <typeparam name="T"> The type of the equatable items.</typeparam>
    /// <param name="list"> The list to manipulate. </param>
    /// <param name="obj"> The object to insert or replace an existing object comparing equal to it with. </param>
    /// <returns> The index of the added or replaced object. </returns>
    public static int AddOrReplace<T>(this List<T> list, T obj) where T : IEquatable<T>
    {
        var idx = list.FindIndex(obj.Equals);
        if (idx < 0)
        {
            list.Add(obj);
            return list.Count - 1;
        }

        list[idx] = obj;
        return idx;
    }

    /// <param name="list"> The list to manipulate. </param>
    /// <typeparam name="T"> The type of the equatable items.</typeparam>
    extension<T>(IList<T> list)
    {
        /// <summary> Move an item in a list from index 1 to index 2. </summary>
        /// <param name="idx1"> The index to move the item from. </param>
        /// <param name="idx2"> The index to move the item to. </param>
        /// <returns> Whether the list changed. </returns>
        /// <remarks>
        ///   The indices are clamped to the valid range. <br/>
        ///   Other list entries are shifted accordingly.
        /// </remarks>
        public bool Move(int idx1, int idx2)
        {
            idx1 = Math.Clamp(idx1, 0, list.Count - 1);
            idx2 = Math.Clamp(idx2, 0, list.Count - 1);
            if (idx1 == idx2)
                return false;

            var tmp = list[idx1];
            // move element down and shift other elements up
            if (idx1 < idx2)
                for (var i = idx1; i < idx2; i++)
                    list[i] = list[i + 1];
            // move element up and shift other elements down
            else
                for (var i = idx1; i > idx2; i--)
                    list[i] = list[i - 1];

            list[idx2] = tmp;
            return true;
        }

        /// <summary> Move an item in a list from index 1 to index 2. </summary>
        /// <param name="idx1"> The index to move the item from, updated if it has to be clamped. </param>
        /// <param name="idx2"> The index to move the item to, updated if it has to be clamped. </param>
        /// <returns> Whether the list changed. The indices may change without this being true. </returns>
        /// <remarks>
        ///   The indices are clamped to the valid range and updated. <br/>
        ///   Other list entries are shifted accordingly.
        /// </remarks>
        public bool Move(ref int idx1, ref int idx2)
        {
            idx1 = Math.Clamp(idx1, 0, list.Count - 1);
            idx2 = Math.Clamp(idx2, 0, list.Count - 1);
            if (idx1 == idx2)
                return false;

            var tmp = list[idx1];
            // move element down and shift other elements up
            if (idx1 < idx2)
                for (var i = idx1; i < idx2; i++)
                    list[i] = list[i + 1];
            // move element up and shift other elements down
            else
                for (var i = idx1; i > idx2; i--)
                    list[i] = list[i - 1];

            list[idx2] = tmp;
            return true;
        }

        /// <summary> Ensure that a list has at least a certain number of elements, adding default elements if necessary. </summary>
        /// <param name="count"> The minimum number of items that should be available in the list. </param>
        /// <returns> The number of added default items. </returns>
        public int EnsureCount(int count)
        {
            if (list.Count >= count)
                return 0;

            var toAdd = count - list.Count;
            for (var i = 0; i < toAdd; i++)
                list.Add(default!);
            return toAdd;
        }

        /// <inheritdoc cref="List{T}.RemoveRange"/>
        public void RemoveRange(int index, int count)
        {
            switch (list)
            {
                case List<T> l:           l.RemoveRange(index, count); break;
                case IAdvancedList<T> al: al.RemoveRange(index, count); break;
                default:
                {
                    if (!AdaptRangeIndices(ref index, ref count, list.Count))
                        return;

                    for (var i = index + count - 1; i >= index; --i)
                        list.RemoveAt(index);
                    break;
                }
            }
        }

        /// <inheritdoc cref="List{T}.AddRange"/>
        public void AddRange(IEnumerable<T> items)
        {
            switch (list)
            {
                case List<T> l:           l.AddRange(items); break;
                case IAdvancedList<T> al: al.AddRange(items); break;
                default:
                {
                    foreach (var item in items)
                        list.Add(item);
                    break;
                }
            }
        }

        /// <inheritdoc cref="List{T}.TrimExcess"/>
        public void TrimExcess()
        {
            switch (list)
            {
                case List<T> l:           l.TrimExcess(); break;
                case IAdvancedList<T> al: al.TrimExcess(); break;
                default:                  InvalidType(list.GetType()); break;
            }
        }

        /// <inheritdoc cref="List{T}.EnsureCapacity"/>
        public int EnsureCapacity(int capacity)
        {
            switch (list)
            {
                case List<T> l:           return l.EnsureCapacity(capacity);
                case IAdvancedList<T> al: return al.EnsureCapacity(capacity);
                default:
                    // Do nothing, not supported.
                    InvalidType(list.GetType());
                    return capacity;
            }
        }
    }

    /// <summary> Remove all duplicate elements in a list. </summary>
    /// <typeparam name="T"> The type of the elements. </typeparam>
    /// <typeparam name="TCompare"> The equatable base type of the elements. </typeparam>
    /// <param name="list"> The list to remove duplicates from. </param>
    /// <returns> The number of removed elements. </returns>
    /// <remarks> If the equatable items are not truly equal, note that this will keep the last occurence of each item evaluating as equal, not the first. </remarks>
    public static int RemoveDuplicates<T, TCompare>(this IList<T> list) where T : IEquatable<TCompare>, TCompare
    {
        var oldCount = list.Count;
        var set      = new HashSet<T>(list.Count);
        for (var i = list.Count - 1; i >= 0; --i)
        {
            if (!set.Add(list[i]))
                list.RemoveAt(i);
        }

        return oldCount - list.Count;
    }

    /// <inheritdoc cref="RemoveDuplicates{T,TCompare}"/>
    public static int RemoveDuplicates<T>(this IList<T> list) where T : IEquatable<T>
        => list.RemoveDuplicates<T, T>();

    /// <summary> Fix up the indices for a range removal according to the actual size of the container.</summary>
    /// <returns> False if there is nothing to do after fixing. </returns>
    internal static bool AdaptRangeIndices(ref int index, ref int count, int totalCount)
    {
        if (count <= 0 || totalCount is 0)
            return false;

        if (index < 0)
        {
            count += index;
            index =  0;
        }
        else if (index > totalCount)
        {
            return false;
        }

        var diff = totalCount - index;
        if (diff < count)
            count = diff;

        if (count <= 0)
            return false;

        return true;
    }

    [Conditional("DEBUG")]
    private static void InvalidType(Type type, [CallerMemberName] string? caller = null)
        => throw new ArgumentException($"{caller} can not be called with a list of type {type.Name}.");
}
