namespace Luna;

/// <summary> A predicate with an additional context to consider. </summary>
/// <typeparam name="T"> The type of the object to check. </typeparam>
/// <typeparam name="TContext"> The type of the context. </typeparam>
/// <param name="value"> The value to check. </param>
/// <param name="context"> The context to use while checking. </param>
/// <returns> True if the predicate is fulfilled, false otherwise. </returns>
public delegate bool ContextPredicate<in T, TContext>(T value, in TContext context);

public static class IndexOfExtensions
{
    /// <param name="array"> The list of objects to search. </param>
    extension<T>(IEnumerable<T> array)
    {
        /// <summary> Find the index of the first object fulfilling <paramref name="predicate"/>'s criteria in <paramref name="array"/>. </summary>
        /// <param name="context"> Additional context for static lambda optimization. </param>
        /// <param name="predicate"> The predicate to check the objects, using the context. </param>
        /// <param name="from"> Objects up to this index are skipped. </param>
        /// <returns> -1 if no object is found, otherwise the index. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf<TContext>(in TContext context, Func<T, TContext, bool> predicate, int from = 0)
        {
            var i = 0;
            foreach (var obj in array)
            {
                if (i++ < from)
                    continue;

                if (predicate(obj, context))
                    return i - 1;
            }

            return -1;
        }

        /// <inheritdoc cref="IndexOf{TContext}(IEnumerable{T},in TContext,Func{T,TContext,bool},int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf<TContext>(in TContext context, ContextPredicate<T, TContext> predicate, int from = 0)
        {
            var i = 0;
            foreach (var obj in array)
            {
                if (i++ < from)
                    continue;

                if (predicate(obj, context))
                    return i - 1;
            }

            return -1;
        }

        /// <inheritdoc cref="IndexOf{TContext}(IEnumerable{T},in TContext,Func{T,TContext,bool},int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(Predicate<T> predicate, int from = 0)
        {
            var i = 0;
            foreach (var obj in array)
            {
                if (i++ < from)
                    continue;

                if (predicate(obj))
                    return i - 1;
            }

            return -1;
        }
    }

    /// <param name="array"> The list of objects to search. </param>
    extension<T>(T[] array)
    {
        /// <inheritdoc cref="IndexOf{T,TContext}(IEnumerable{T},in TContext,Func{T,TContext,bool},int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(Predicate<T> predicate, int from = 0)
        {
            for (var i = Math.Max(0, from); i < array.Length; ++i)
            {
                if (predicate(array[i]))
                    return i;
            }

            return -1;
        }

        /// <inheritdoc cref="IndexOf{T,TContext}(IEnumerable{T},in TContext,Func{T,TContext,bool},int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf<TContext>(in TContext context, Func<T, TContext, bool> predicate, int from = 0)
        {
            for (var i = Math.Max(0, from); i < array.Length; ++i)
            {
                if (predicate(array[i], context))
                    return i;
            }

            return -1;
        }

        /// <inheritdoc cref="IndexOf{T,TContext}(IEnumerable{T},in TContext,Func{T,TContext,bool},int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf<TContext>(in TContext context, ContextPredicate<T, TContext> predicate, int from = 0)
        {
            for (var i = Math.Max(0, from); i < array.Length; ++i)
            {
                if (predicate(array[i], context))
                    return i;
            }

            return -1;
        }
    }

    /// <param name="array"> The list of objects to search. </param>
    extension<T>(List<T> array)
    {
        /// <inheritdoc cref="IndexOf{T,TContext}(IEnumerable{T},in TContext,Func{T,TContext,bool},int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(Predicate<T> predicate, int from = 0)
        {
            for (var i = Math.Max(0, from); i < array.Count; ++i)
            {
                if (predicate(array[i]))
                    return i;
            }

            return -1;
        }

        /// <inheritdoc cref="IndexOf{T,TContext}(IEnumerable{T},in TContext,Func{T,TContext,bool},int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf<TContext>(in TContext context, Func<T, TContext, bool> predicate, int from = 0)
        {
            for (var i = Math.Max(0, from); i < array.Count; ++i)
            {
                if (predicate(array[i], context))
                    return i;
            }

            return -1;
        }

        /// <inheritdoc cref="IndexOf{T,TContext}(IEnumerable{T},in TContext,Func{T,TContext,bool},int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf<TContext>(in TContext context, ContextPredicate<T, TContext> predicate, int from = 0)
        {
            for (var i = Math.Max(0, from); i < array.Count; ++i)
            {
                if (predicate(array[i], context))
                    return i;
            }

            return -1;
        }
    }

    extension<T>(IEnumerable<T> array) where T : notnull
    {
        /// <summary> Find the index of the first occurrence of <paramref name="needle"/> in <paramref name="array"/>. </summary>
        /// <returns> -1 if <paramref name="needle"/> is not contained, otherwise its index. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(T needle, int from = 0)
        {
            var i = 0;
            foreach (var obj in array)
            {
                if (i++ < from)
                    continue;

                if (needle.Equals(obj))
                    return i - 1;
            }

            return -1;
        }
    }

    extension<T>(T[] array) where T : notnull
    {
        /// <inheritdoc cref="IndexOf{T}(IEnumerable{T},T,int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(T needle, int from = 0)
        {
            for (var i = Math.Max(0, from); i < array.Length; ++i)
            {
                if (array[i].Equals(needle))
                    return i;
            }

            return -1;
        }
    }

    extension<T>(List<T> array) where T : notnull
    {
        /// <inheritdoc cref="IndexOf{T}(IEnumerable{T},T,int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(T needle, int from = 0)
        {
            for (var i = Math.Max(0, from); i < array.Count; ++i)
            {
                if (array[i].Equals(needle))
                    return i;
            }

            return -1;
        }
    }
}
