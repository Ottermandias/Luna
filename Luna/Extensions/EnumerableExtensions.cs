namespace Luna;

public static class EnumerableExtensions
{
    /// <summary> Remove an added index from an indexed enumerable. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static IEnumerable<T> WithoutIndex<T>(this IEnumerable<(int Index, T Value)> list)
        => list.Select(x => x.Value);

    /// <summary> Remove the value and only keep the index from an indexed enumerable. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static IEnumerable<int> WithoutValue<T>(this IEnumerable<(int Index, T Value)> list)
        => list.Select(x => x.Index);

    /// <summary> Find the index of the first object fulfilling predicate's criteria in <paramref name="array"/>. </summary>
    /// <returns> -1 if no object is found, otherwise the index. </returns>
    public static int IndexOf<T>(this IEnumerable<T> array, Predicate<T> predicate)
    {
        var i = 0;
        foreach (var obj in array)
        {
            if (predicate(obj))
                return i;

            ++i;
        }

        return -1;
    }

    /// <summary> Find the index of the first occurrence of <paramref name="needle"/> in <paramref name="array"/>. </summary>
    /// <returns> -1 if <paramref name="needle"/> is not contained, otherwise its index. </returns>
    public static int IndexOf<T>(this IEnumerable<T> array, T needle) where T : notnull
    {
        var i = 0;
        foreach (var obj in array)
        {
            if (needle.Equals(obj))
                return i;

            ++i;
        }

        return -1;
    }

    /// <summary> Find the first object fulfilling <paramref name="predicate"/>'s criteria in <paramref name="array"/>>, if one exists. </summary>
    /// <returns> True if an object is found, false otherwise. </returns>
    public static bool FindFirst<T>(this IEnumerable<T> array, Predicate<T> predicate, [NotNullWhen(true)] out T? result)
    {
        foreach (var obj in array)
        {
            if (!predicate(obj))
                continue;

            result = obj!;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary> Find the first occurrence of <paramref name="needle"/> in <paramref name="array"/> and return the value contained in the list in result. </summary>
    /// <returns> True if <paramref name="needle"/> is found, false otherwise. </returns>
    public static bool FindFirst<T>(this IEnumerable<T> array, T needle, [NotNullWhen(true)] out T? result) where T : notnull
    {
        foreach (var obj in array)
        {
            if (!obj.Equals(needle))
                continue;

            result = obj;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary> Transform an enumerable while filtering it at the same time. </summary>
    /// <typeparam name="TIn"> The type of the input objects. </typeparam>
    /// <typeparam name="TOut"> The type of the transformed objects. </typeparam>
    /// <param name="enumerable"> The input objects. </param>
    /// <param name="filterMap"> A function that transforms the input objects into the output objects and also returns a bool whether the output is valid. </param>
    /// <returns> An enumeration of the filtered output objects. </returns>
    public static IEnumerable<TOut> SelectWhere<TIn, TOut>(this IEnumerable<TIn> enumerable, Func<TIn, (bool, TOut?)> filterMap)
    {
        foreach (var obj in enumerable)
        {
            var (valid, transform) = filterMap(obj);
            if (valid)
                yield return transform!;
        }
    }
}
