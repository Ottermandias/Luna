namespace Luna;

/// <summary> A container that usually contains zero or one reference-typed items but may contain more. </summary>
/// <typeparam name="T"> The type of the object(s) contained. </typeparam>
public readonly struct SingleArray<T> : IReadOnlyList<T> where T : notnull
{
    /// <summary> Reference type as a union of the object type itself and an array of objects. </summary>
    private readonly object? _value;

    /// <summary> Create an empty single array containing no objects. </summary>
    public SingleArray()
        => _value = null;

    /// <summary> Create a single array containing an arbitrary number of reference-typed values. </summary>
    /// <param name="values"> The values to contain. </param>
    [OverloadResolutionPriority(50)]
    public SingleArray(params T[] values)
        => _value = values.Length switch
        {
            0 => null,
            1 => values[0],
            _ => values,
        };

    /// <summary> Create a single array containing an arbitrary number of reference-typed values. </summary>
    /// <param name="values"> The values to contain. </param>
    [OverloadResolutionPriority(0)]
    public SingleArray(IEnumerable<T> values)
    {
        if (values is IReadOnlyCollection<T> col)
        {
            if (col.Count is 0)
                _value = null;
            else if (col.Count is 1)
                _value = col.First();
            else if (col is T[] array)
                _value = array;
        }
        else
        {
            var array = values.ToArray();
            _value = array.Length switch
            {
                0 => null,
                1 => array[0],
                _ => array,
            };
        }
    }

    [OverloadResolutionPriority(100)]
    public SingleArray(T? value)
        => _value = value;

    /// <summary> Get the number of contained elements. </summary>
    public int Count
        => _value switch
        {
            T     => 1,
            T[] l => l.Length,
            _     => 0,
        };

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
    {
        switch (_value)
        {
            case T v: yield return v; break;
            case T[] l:
            {
                foreach (var vl in l)
                    yield return vl;

                break;
            }
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public T this[int index]
    {
        get
        {
            return _value switch
            {
                T v when index == 0 => v,
                T[] l               => l[index],
                _                   => throw new IndexOutOfRangeException(),
            };
        }
    }

    /// <summary> Create a new single array by appending a new value. </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A new single array containing all values contained in this one and the appended one at the end. </returns>
    public SingleArray<T> Append(T value)
    {
        return _value switch
        {
            T v                   => new SingleArray<T>(v, value),
            T[] { Length: > 0 } l => new SingleArray<T>(l.Append(value)),
            _                     => new SingleArray<T>(value),
        };
    }

    /// <summary> Create a new single array by prepending a new value. </summary>
    /// <param name="value"> The value to append. </param>
    /// <returns> A new single array containing all values contained in this one and the prepended one at the start. </returns>
    public SingleArray<T> Prepend(T value)
    {
        return _value switch
        {
            T v                   => new SingleArray<T>(v, value),
            T[] { Length: > 0 } l => new SingleArray<T>(l.Prepend(value)),
            _                     => new SingleArray<T>(value),
        };
    }

    /// <summary> Remove all values referring to the same object from the single array. </summary>
    /// <param name="value"> The reference-typed value to remove. </param>
    /// <returns> A new single array containing all values except the removed one in the same order. </returns>
    public SingleArray<T> Remove(T value)
        => Remove(v => ReferenceEquals(v, value));

    /// <summary> Remove all values fulfilling the predicate from the single array. </summary>
    /// <param name="predicate"> The predicate to check for removal. </param>
    /// <returns> A new single array containing all values except the removed one in the same order. </returns>
    public SingleArray<T> Remove(Func<T, bool> predicate)
    {
        return _value switch
        {
            T v when predicate(v)       => new SingleArray<T>(),
            T[] l when l.Any(predicate) => new SingleArray<T>(l.Where(v => !predicate(v))),
            _                           => this,
        };
    }
}
