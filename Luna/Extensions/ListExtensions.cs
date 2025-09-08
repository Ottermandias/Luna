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

    /// <summary> Move an item in a list from index 1 to index 2. </summary>
    /// <typeparam name="T"> The type of the equatable items.</typeparam>
    /// <param name="list"> The list to manipulate. </param>
    /// <param name="idx1"> The index to move the item from. </param>
    /// <param name="idx2"> The index to move the item to. </param>
    /// <returns> Whether the list changed. </returns>
    /// <remarks>
    ///   The indices are clamped to the valid range. <br/>
    ///   Other list entries are shifted accordingly.
    /// </remarks>
    public static bool Move<T>(this IList<T> list, int idx1, int idx2)
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
    /// <typeparam name="T"> The type of the equatable items.</typeparam>
    /// <param name="list"> The list to manipulate. </param>
    /// <param name="idx1"> The index to move the item from, updated if it has to be clamped. </param>
    /// <param name="idx2"> The index to move the item to, updated if it has to be clamped. </param>
    /// <returns> Whether the list changed. The indices may change without this being true. </returns>
    /// <remarks>
    ///   The indices are clamped to the valid range and updated. <br/>
    ///   Other list entries are shifted accordingly.
    /// </remarks>
    public static bool Move<T>(this IList<T> list, ref int idx1, ref int idx2)
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
}
