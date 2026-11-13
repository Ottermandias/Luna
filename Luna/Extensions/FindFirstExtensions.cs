namespace Luna;

public static class FindFirstExtensions
{
    /// <param name="array"> The list of objects to search. </param>
    extension<T>(IEnumerable<T> array)
    {
        /// <inheritdoc cref="FindFirst{T,TContext}(IEnumerable{T},in TContext,ContextPredicate{T,TContext},out T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FindFirst(Predicate<T> predicate, [NotNullWhen(true)] out T? result)
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

        /// <inheritdoc cref="FindFirst{T,TContext}(IEnumerable{T},in TContext,ContextPredicate{T,TContext},out T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FindFirst<TContext>(in TContext context, Func<T, TContext, bool> predicate, [NotNullWhen(true)] out T? result)
        {
            foreach (var obj in array)
            {
                if (!predicate(obj, context))
                    continue;

                result = obj!;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary> Find the first object fulfilling <paramref name="predicate"/>'s criteria in <paramref name="array"/>>, if one exists. </summary>
        /// <param name="context"> Additional context for static lambda optimization. </param>
        /// <param name="predicate"> The predicate to check the objects, using the context. </param>
        /// <param name="result"> The first object matching the predicate, if any. </param>
        /// <returns> True if an object is found, false otherwise. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FindFirst<TContext>(in TContext context, ContextPredicate<T, TContext> predicate, [NotNullWhen(true)] out T? result)
        {
            foreach (var obj in array)
            {
                if (!predicate(obj, context))
                    continue;

                result = obj!;
                return true;
            }

            result = default;
            return false;
        }
    }

    /// <param name="array"> The list of objects to search. </param>
    extension<T>(T[] array)
    {
        /// <inheritdoc cref="FindFirst{T,TContext}(IEnumerable{T},in TContext,ContextPredicate{T,TContext},out T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FindFirst(Predicate<T> predicate, [NotNullWhen(true)] out T? result)
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

        /// <inheritdoc cref="FindFirst{T,TContext}(IEnumerable{T},in TContext,ContextPredicate{T,TContext},out T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FindFirst<TContext>(in TContext context, Func<T, TContext, bool> predicate, [NotNullWhen(true)] out T? result)
        {
            foreach (var obj in array)
            {
                if (!predicate(obj, context))
                    continue;

                result = obj!;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary> Find the first object fulfilling <paramref name="predicate"/>'s criteria in <paramref name="array"/>>, if one exists. </summary>
        /// <param name="context"> Additional context for static lambda optimization. </param>
        /// <param name="predicate"> The predicate to check the objects, using the context. </param>
        /// <param name="result"> The first object matching the predicate, if any. </param>
        /// <returns> True if an object is found, false otherwise. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FindFirst<TContext>(in TContext context, ContextPredicate<T, TContext> predicate, [NotNullWhen(true)] out T? result)
        {
            foreach (var obj in array)
            {
                if (!predicate(obj, context))
                    continue;

                result = obj!;
                return true;
            }

            result = default;
            return false;
        }
    }

    /// <param name="array"> The list of objects to search. </param>
    extension<T>(List<T> array)
    {
        /// <inheritdoc cref="FindFirst{T,TContext}(IEnumerable{T},in TContext,ContextPredicate{T,TContext},out T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FindFirst(Predicate<T> predicate, [NotNullWhen(true)] out T? result)
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

        /// <inheritdoc cref="FindFirst{T,TContext}(IEnumerable{T},in TContext,ContextPredicate{T,TContext},out T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FindFirst<TContext>(in TContext context, Func<T, TContext, bool> predicate, [NotNullWhen(true)] out T? result)
        {
            foreach (var obj in array)
            {
                if (!predicate(obj, context))
                    continue;

                result = obj!;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary> Find the first object fulfilling <paramref name="predicate"/>'s criteria in <paramref name="array"/>>, if one exists. </summary>
        /// <param name="context"> Additional context for static lambda optimization. </param>
        /// <param name="predicate"> The predicate to check the objects, using the context. </param>
        /// <param name="result"> The first object matching the predicate, if any. </param>
        /// <returns> True if an object is found, false otherwise. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FindFirst<TContext>(in TContext context, ContextPredicate<T, TContext> predicate, [NotNullWhen(true)] out T? result)
        {
            foreach (var obj in array)
            {
                if (!predicate(obj, context))
                    continue;

                result = obj!;
                return true;
            }

            result = default;
            return false;
        }
    }

    extension<T>(IEnumerable<T> array) where T : notnull
    {
        /// <summary> Find the first occurrence of <paramref name="needle"/> in <paramref name="array"/> and return the value contained in the list in result. </summary>
        /// <returns> True if <paramref name="needle"/> is found, false otherwise. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FindFirst(T needle, [NotNullWhen(true)] out T? result)
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
    }

    extension<T>(T[] array) where T : notnull
    {
        /// <inheritdoc cref="FindFirst{T}(IEnumerable{T},T,out T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FindFirst(T needle, [NotNullWhen(true)] out T? result)
        {
            foreach (var value in array)
            {
                if (!value.Equals(needle))
                    continue;

                result = value;
                return true;
            }

            result = default;
            return false;
        }
    }

    extension<T>(List<T> array) where T : notnull
    {
        /// <inheritdoc cref="FindFirst{T}(IEnumerable{T},T,out T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FindFirst(T needle, [NotNullWhen(true)] out T? result)
        {
            foreach (var value in array)
            {
                if (!value.Equals(needle))
                    continue;

                result = value;
                return true;
            }

            result = default;
            return false;
        }
    }
}
