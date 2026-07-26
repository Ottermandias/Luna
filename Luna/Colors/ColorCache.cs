using ImSharp.ImNodes;

namespace Luna;

/// <typeparam name="TColorId"> A contiguous enumeration of color IDs. </typeparam>
/// <typeparam name="TColorData"> A provider to emulate the color ID implementing an interface to get default data and information. </typeparam>
/// <remarks> This automatically subscribes to <see cref="ImSharpPerFrame.Update"/> and <see cref="ImSharpDalamudContext.StyleChanged"/> and is always up-to-date. </remarks>
public sealed class ColorCache<TColorId, TColorData> : IDisposable
    where TColorId : unmanaged, Enum
    where TColorData : IColorData<TColorId>
{
    private readonly ColorDictionary<TColorId, TColorData> _customStorage;
    private readonly (Vector4 Vector, Rgba32 Color)[] _imgui = new (Vector4 Vector, Rgba32 Color)[ImGuiColor.Values.Count];
    private readonly (Vector4 Vector, Rgba32 Color)[] _imNodes = new (Vector4 Vector, Rgba32 Color)[ImNodesColor.Values.Count];
    private readonly (Vector4 Vector, Rgba32 Color)[] _dalamud = new (Vector4 Vector, Rgba32 Color)[DalamudColor.Values.Count];
    private readonly (Vector4 Vector, Rgba32 Color)[] _luna = new (Vector4 Vector, Rgba32 Color)[LunaColor.Values.Count];
    private readonly (Vector4 Vector, Rgba32 Color)[] _colors = new (Vector4 Vector, Rgba32 Color)[EnumExtensions.get_Values<TColorId>().Count];
    private          bool _dirty = true;

    /// <summary> Get the value for the given ImGui color as Rgba32. </summary>
    public Rgba32 this[ImGuiColor id]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => _imgui[(int)id].Color;
    }

    /// <summary> Get the value for the given ImNodes color as Rgba32. </summary>
    public Rgba32 this[ImNodesColor id]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => _imNodes[(int)id].Color;
    }

    /// <summary> Get the value for the given Dalamud color as Rgba32. </summary>
    public Rgba32 this[DalamudColor id]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => _dalamud[(int)id].Color;
    }

    /// <summary> Get the value for the given Luna color as Rgba32. </summary>
    public Rgba32 this[LunaColor id]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => _luna[(int)id].Color;
    }

    /// <summary> Get the value for the given color ID as a vector of 4 floats. </summary>
    public Rgba32 this[TColorId id]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => _colors[ToIndex(id)].Color;
    }

    /// <summary> Get the value for the given ImGui color as a vector of 4 floats. </summary>
    public Vector4 this[ImGuiColor id, bool _]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => _imgui[(int)id].Vector;
    }

    /// <summary> Get the value for the given ImNodes color as a vector of 4 floats. </summary>
    public Vector4 this[ImNodesColor id, bool _]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => _imNodes[(int)id].Vector;
    }

    /// <summary> Get the value for the given Dalamud color as a vector of 4 floats. </summary>
    public Vector4 this[DalamudColor id, bool _]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => _dalamud[(int)id].Vector;
    }

    /// <summary> Get the value for the given Luna color as a vector of 4 floats. </summary>
    public Vector4 this[LunaColor id, bool _]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => _luna[(int)id].Vector;
    }

    /// <summary> Get the value for the given color ID as a vector of 4 floats. </summary>
    public Vector4 this[TColorId id, bool _]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => _colors[ToIndex(id)].Vector;
    }

    /// <summary> Get the value for the given ImGui color as a vector of 4 floats. </summary>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public Vector4 AsVector(ImGuiColor id)
        => _imgui[(int)id].Vector;

    /// <summary> Get the value for the given ImNodes color as a vector of 4 floats. </summary>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public Vector4 AsVector(ImNodesColor id)
        => _imNodes[(int)id].Vector;

    /// <summary> Get the value for the given Dalamud color as a vector of 4 floats. </summary>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public Vector4 AsVector(DalamudColor id)
        => _dalamud[(int)id].Vector;

    /// <summary> Get the value for the given Luna color as a vector of 4 floats. </summary>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public Vector4 AsVector(LunaColor id)
        => _luna[(int)id].Vector;

    /// <summary> Get the value for the given color ID as a vector of 4 floats. </summary>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public Vector4 AsVector(TColorId id)
        => _colors[ToIndex(id)].Vector;

    /// <summary> Create a new color cache based on the given custom color ID and its storage. </summary>
    /// <param name="customStorage"> The storage for custom colors. </param>
    public ColorCache(ColorDictionary<TColorId, TColorData> customStorage)
    {
        _customStorage                     =  customStorage;
        _customStorage.Change              += SetDirty;
        ImSharpDalamudContext.StyleChanged += SetDirty;
        ImSharpPerFrame.Update             += Update;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _customStorage.Change              -= SetDirty;
        ImSharpDalamudContext.StyleChanged -= SetDirty;
        ImSharpPerFrame.Update             -= Update;
    }

    /// <summary> Update all cached values. </summary>
    private void Update()
    {
        if (!_dirty)
            return;

        _dirty = false;
        foreach (var color in ImGuiColor.Values)
            _imgui[(int)color] = (Im.Style[color], color.Get());

        if (ImNodes.Initialized)
            foreach (var color in ImNodesColor.Values)
            {
                var value = ImNodes.Style[color];
                _imNodes[(int)color] = (value.ToVector(), value);
            }

        foreach (var color in DalamudColor.Values)
        {
            var value = color.Value;
            _dalamud[(int)color] = (value, value);
        }

        foreach (var color in LunaColor.Values)
        {
            var value = color.Value;
            _luna[(int)color] = (value, value);
        }

        foreach (var color in EnumExtensions.get_Values<TColorId>())
        {
            var value = _customStorage.GetColor(color);
            _colors[ToIndex(color)] = (value.ToVector(), value);
        }
    }

    private void SetDirty()
        => _dirty = true;

    [MethodImpl(ImSharpConfiguration.OptInl)]
    private static unsafe int ToIndex(TColorId id)
        => *(int*)&id;
}
