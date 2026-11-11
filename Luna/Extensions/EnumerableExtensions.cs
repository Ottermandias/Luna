using Luna.Generators;

namespace Luna;

[GenerateArities(9, IncludeZeroArity = true)]
public delegate bool TryFunc<in T1, TRet>(T1 a1, out TRet? ret);

public static class EnumerableExtensions
{
    extension<T>(IEnumerable<(int Index, T Value)> list)
    {
        /// <summary> Remove an added index from an indexed enumerable. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> WithoutIndex()
            => list.Select(static x => x.Value);

        /// <summary> Remove the value and only keep the index from an indexed enumerable. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<int> WithoutValue()
            => list.Select(static x => x.Index);
    }

    /// <param name="enumerable"> The input objects. </param>
    /// <typeparam name="TIn"> The type of the input objects. </typeparam>
    extension<TIn>(IEnumerable<TIn> enumerable)
    {
        /// <summary> Transform an enumerable while filtering it at the same time. </summary>
        /// <typeparam name="TOut"> The type of the transformed objects. </typeparam>
        /// <param name="filterMap"> A function that transforms the input objects into the output objects and also returns a bool whether the output is valid. </param>
        /// <returns> An enumeration of the filtered output objects. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        [OverloadResolutionPriority(50)]
        public IEnumerable<TOut> SelectWhere<TOut>(Func<TIn, (bool, TOut?)> filterMap)
        {
            foreach (var obj in enumerable)
            {
                var (valid, transform) = filterMap(obj);
                if (valid)
                    yield return transform!;
            }
        }

        /// <summary> Transform an enumerable while filtering it at the same time. </summary>
        /// <typeparam name="TOut"> The type of the transformed objects. </typeparam>
        /// <param name="filterMap"> A function that transforms the input objects into the output objects and returns a bool whether the output is valid. </param>
        /// <returns> An enumeration of the filtered output objects. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        [OverloadResolutionPriority(100)]
        public IEnumerable<TOut?> SelectWhere<TOut>(TryFunc<TIn, TOut> filterMap)
        {
            foreach (var obj in enumerable)
            {
                if (filterMap(obj, out var ret))
                    yield return ret;
            }
        }

        /// <summary> Compute the maximum value in a set, or return a default value if the set is empty. </summary>
        /// <typeparam name="TValue"> The type of the value to compare. </typeparam>
        /// <param name="selector"> The selector to reduce the objects to a specific value. </param>
        /// <param name="ifEmpty"> The default value to return if the set of objects is empty. </param>
        /// <returns> The maximum selected value in the set or <paramref name="ifEmpty"/> if there are no elements in the set. </returns>
        public TValue Max<TValue>(Func<TIn, TValue> selector, TValue ifEmpty)
            where TValue : IComparable<TValue>
        {
            using var enumerator = enumerable.GetEnumerator();
            if (!enumerator.MoveNext())
                return ifEmpty;

            var comparer = Comparer<TValue>.Default;
            var max      = selector(enumerator.Current);
            while (enumerator.MoveNext())
            {
                var value = selector(enumerator.Current);
                if (comparer.Compare(max, value) < 0)
                    max = value;
            }

            return max;
        }

        /// <summary> Compute the minimum value in a set, or return a default value if the set is empty. </summary>
        /// <typeparam name="TValue"> The type of the value to compare. </typeparam>
        /// <param name="selector"> The selector to reduce the objects to a specific value. </param>
        /// <param name="ifEmpty"> The default value to return if the set of objects is empty. </param>
        /// <returns> The minimum selected value in the set or <paramref name="ifEmpty"/> if there are no elements in the set. </returns>
        public TValue Min<TValue>(Func<TIn, TValue> selector, TValue ifEmpty)
            where TValue : IComparable<TValue>
        {
            using var enumerator = enumerable.GetEnumerator();
            if (!enumerator.MoveNext())
                return ifEmpty;

            var comparer = Comparer<TValue>.Default;
            var min      = selector(enumerator.Current);
            while (enumerator.MoveNext())
            {
                var value = selector(enumerator.Current);
                if (comparer.Compare(min, value) > 0)
                    min = value;
            }

            return min;
        }
    }
}
