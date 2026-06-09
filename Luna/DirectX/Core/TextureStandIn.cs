using Dalamud.Interface.Textures.TextureWraps;

namespace Luna.DirectX;

/// <summary> A texture that can be passed to a shader or drawn to an ImGui surface. Either pre-determined, or fetched from a list. </summary>
/// <remarks> This union facilitates dependency tracking, for render graphs. </remarks>
public readonly struct TextureStandIn : IEquatable<TextureStandIn>
{
    private readonly object? _source;
    private readonly int     _index;

    /// <summary> Gets the texture ID for use in ImSharp or in DirectX. </summary>
    public ImTextureId Id
        => _source switch
        {
            IDalamudTextureWrap wrap               => wrap.Id,
            IReadOnlyList<ImTextureId> source      => source[_index],
            IReadOnlyList<TextureStandIn> indirect => indirect[_index].Id,
            _                                      => default,
        };

    /// <summary> Whether this object is empty (<c>default</c>). </summary>
    public bool IsEmpty
        => _source is null;

    /// <summary> Creates a <see cref="TextureStandIn"/> from a pre-determined texture. </summary>
    /// <param name="wrap"> The texture. </param>
    public TextureStandIn(IDalamudTextureWrap wrap)
    {
        _source = wrap;
        _index  = 0;
    }

    /// <summary> Creates a <see cref="TextureStandIn"/> from a list item specification. </summary>
    /// <param name="list"> The list from which the texture is to be fetched. </param>
    /// <param name="index"> The index of the texture in <paramref name="list"/>. </param>
    public TextureStandIn(IReadOnlyList<ImTextureId> list, int index)
    {
        _source = list;
        _index  = index;
    }

    /// <summary> Creates an indirect <see cref="TextureStandIn"/> from a target specification. </summary>
    /// <param name="list"> The list that contains the target. </param>
    /// <param name="index"> The index of the target in <paramref name="list"/>. </param>
    /// <remarks> It is the caller's responsibility to ensure that no cycles are created. </remarks>
    public TextureStandIn(IReadOnlyList<TextureStandIn> list, int index)
    {
        _source = list;
        _index  = index;
    }

    /// <summary> Extracts the pre-determined texture this object was constructed from, if applicable. </summary>
    /// <param name="wrap"> The texture, or <c>null</c> if not applicable. </param>
    /// <returns> Whether this function succeeded. </returns>
    public bool TryGetWrap([NotNullWhen(true)] out IDalamudTextureWrap? wrap)
    {
        switch (_source)
        {
            case IDalamudTextureWrap nativeWrap:
                wrap = nativeWrap;
                return true;
            case ITextureWrapProvider wrapProvider:
                wrap = wrapProvider.GetTextureWrap(_index);
                return wrap is not null;
            case IReadOnlyList<TextureStandIn> indirect:
                return indirect[_index].TryGetWrap(out wrap);
            default:
                wrap = null;
                return false;
        }
    }

    /// <summary> Extracts the list item specification this object was constructed from, if applicable. </summary>
    /// <param name="list"> The list from which the texture is to be fetched, or <c>null</c> if not applicable. </param>
    /// <param name="index"> The index of the texture in <paramref name="list"/>. </param>
    /// <returns> Whether this function succeeded. </returns>
    public bool TryGetListAndIndex([NotNullWhen(true)] out IReadOnlyList<ImTextureId>? list, out int index)
    {
        switch (_source)
        {
            case IReadOnlyList<ImTextureId> source:
                list  = source;
                index = _index;
                return true;
            case IReadOnlyList<TextureStandIn> indirect: return indirect[_index].TryGetListAndIndex(out list, out index);
            default:
                list  = null;
                index = 0;
                return false;
        }
    }

    /// <summary> Extracts the indirection target specification this object was constructed from, if applicable. </summary>
    /// <param name="list"> The list that contains the target, or <c>null</c> if not applicable. </param>
    /// <param name="index"> The index of the target in <paramref name="list"/>. </param>
    /// <returns> Whether this function succeeded. </returns>
    public bool TryGetIndirectionTarget([NotNullWhen(true)] out IReadOnlyList<TextureStandIn>? list, out int index)
    {
        list  = _source as IReadOnlyList<TextureStandIn>;
        index = _index;
        return list is not null;
    }

    /// <summary> Invokes the given action with this texture as a wrap. </summary>
    /// <param name="action"> The action to invoke. </param>
    /// <exception cref="InvalidOperationException"> This object doesn't contain a texture. </exception>
    public void InvokeWithWrap(Action<IDalamudTextureWrap> action)
    {
        if (TryGetWrap(out var wrap))
        {
            action(wrap);
            return;
        }

        var id = Id;
        if (id.IsNull)
            throw new InvalidOperationException("InvokeWithWrap called on an empty TextureStandIn.");

        using var image = new Image(id);
        action(image);
    }

    /// <summary> Invokes the given function with this texture as a wrap. </summary>
    /// <param name="func"> The function to invoke. </param>
    /// <typeparam name="T"> The return type of the function. </typeparam>
    /// <returns> The return value of the function. </returns>
    /// <exception cref="InvalidOperationException"> This object doesn't contain a texture. </exception>
    [OverloadResolutionPriority(-1)]
    public T InvokeWithWrap<T>(Func<IDalamudTextureWrap, T> func)
    {
        if (TryGetWrap(out var wrap))
            return func(wrap);

        var id = Id;
        if (id.IsNull)
            throw new InvalidOperationException("InvokeWithWrap called on an empty TextureStandIn.");

        using var image = new Image(id);
        return func(image);
    }

    /// <summary> Invokes the given action with this texture as a wrap. </summary>
    /// <param name="action"> The action to invoke. </param>
    /// <exception cref="InvalidOperationException"> This object doesn't contain a texture. </exception>
    public Task InvokeWithWrap(Func<IDalamudTextureWrap, Task> action)
    {
        if (TryGetWrap(out var wrap))
            return action(wrap);

        var id = Id;
        if (id.IsNull)
            throw new InvalidOperationException("InvokeWithWrap called on an empty TextureStandIn.");

        return DoInvokeWithWrap(action, id);
    }

    /// <summary> Invokes the given function with this texture as a wrap. </summary>
    /// <param name="func"> The function to invoke. </param>
    /// <typeparam name="T"> The return type of the function. </typeparam>
    /// <returns> The return value of the function. </returns>
    /// <exception cref="InvalidOperationException"> This object doesn't contain a texture. </exception>
    public Task<T> InvokeWithWrap<T>(Func<IDalamudTextureWrap, Task<T>> func)
    {
        if (TryGetWrap(out var wrap))
            return func(wrap);

        var id = Id;
        if (id.IsNull)
            throw new InvalidOperationException("InvokeWithWrap called on an empty TextureStandIn.");

        return DoInvokeWithWrap(func, id);
    }

    private static async Task DoInvokeWithWrap(Func<IDalamudTextureWrap, Task> action, ImTextureId id)
    {
        using var image = new Image(id);
        await action(image).ConfigureAwait(false);
    }

    private static async Task<T> DoInvokeWithWrap<T>(Func<IDalamudTextureWrap, Task<T>> func, ImTextureId id)
    {
        using var image = new Image(id);
        return await func(image).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is TextureStandIn other && Equals(other);

    /// <inheritdoc/>
    public bool Equals(TextureStandIn other)
        => Equals(_source, other._source) && _index == other._index;

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(_source, _index);

    public static implicit operator ImTextureId(TextureStandIn input)
        => input.Id;

    public static bool operator ==(TextureStandIn lhs, TextureStandIn rhs)
        => lhs.Equals(rhs);

    public static bool operator !=(TextureStandIn lhs, TextureStandIn rhs)
        => !lhs.Equals(rhs);
}
