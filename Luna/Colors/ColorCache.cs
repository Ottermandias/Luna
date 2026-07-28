using ImSharp.ImNodes;

namespace Luna;

/// <typeparam name="TColorId"> A contiguous enumeration of color IDs. </typeparam>
/// <typeparam name="TColorData"> A provider to emulate the color ID implementing an interface to get default data and information. </typeparam>
/// <remarks> This automatically subscribes to <see cref="ImSharpPerFrame.Update"/> and <see cref="ImSharpDalamudContext.StyleChanged"/> and is always up-to-date. </remarks>
public sealed class ColorCache<TColorId, TColorData> : IDisposable
    where TColorId : unmanaged, Enum
    where TColorData : IColorData<TColorId>
{
    private static readonly Rgba32  ErrorColor  = Rgba32.Magenta;
    private static readonly Vector4 ErrorVector = ErrorColor.ToVector();

    private readonly ColorDictionary<TColorId, TColorData> _customStorage;

    private readonly Rgba32[]                         _imgui   = GetInitialized(ImGuiColor.Values.Count,   ErrorColor);
    private readonly Vector4[]                        _imNodes = GetInitialized(ImNodesColor.Values.Count, ErrorVector);
    private readonly (Vector4 Vector, Rgba32 Color)[] _dalamud = GetInitialized(DalamudColor.Values.Count, (ErrorVector, ErrorColor));
    private readonly (Vector4 Vector, Rgba32 Color)[] _luna    = GetInitialized(LunaColor.Values.Count,    (ErrorVector, ErrorColor));

    private readonly (Vector4 Vector, Rgba32 Color)[] _colors = GetInitialized(EnumExtensions.get_Values<TColorId>().Count,
        (ErrorVector, ErrorColor));

    private bool _anyDirty     = true;
    private bool _imguiDirty   = true;
    private bool _imNodesDirty = true;
    private bool _dalamudDirty = true;
    private bool _lunaDirty    = true;
    private bool _customDirty  = true;

    /// <summary> Get the value for the given ImGui color as Rgba32. </summary>
    public Rgba32 this[ImGuiColor id]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => _imguiDirty ? id.Get() : _imgui[(int)id];
    }

    /// <summary> Get the value for the given ImNodes color as Rgba32. </summary>
    public Rgba32 this[ImNodesColor id]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => ImNodes.Style[id];
    }

    /// <summary> Get the value for the given Dalamud color as Rgba32. </summary>
    public Rgba32 this[DalamudColor id]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => _dalamudDirty ? id.Value : _dalamud[(int)id].Color;
    }

    /// <summary> Get the value for the given Luna color as Rgba32. </summary>
    public Rgba32 this[LunaColor id]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => _lunaDirty ? id.Value : _luna[(int)id].Color;
    }

    /// <summary> Get the value for the given color ID as a vector of 4 floats. </summary>
    public Rgba32 this[TColorId id]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => _customDirty ? _customStorage.GetColor(id) : _colors[ToIndex(id)].Color;
    }

    /// <summary> Get the value for the given ImGui color as a vector of 4 floats. </summary>
    /// <remarks> Checks for initialization before obtaining colors. </remarks>
    public Vector4 this[ImGuiColor id, bool _]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => Im.Style[id];
    }

    /// <summary> Get the value for the given ImNodes color as a vector of 4 floats. </summary>
    /// <remarks> Checks for initialization before obtaining colors. </remarks>
    public Vector4 this[ImNodesColor id, bool _]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => _imNodesDirty ? ImNodes.Style[id].ToVector() : _imNodes[(int)id];
    }

    /// <summary> Get the value for the given Dalamud color as a vector of 4 floats. </summary>
    /// <remarks> Is always initialized. </remarks>
    public Vector4 this[DalamudColor id, bool _]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => _dalamudDirty ? id.Value : _dalamud[(int)id].Vector;
    }

    /// <summary> Get the value for the given Luna color as a vector of 4 floats. </summary>
    public Vector4 this[LunaColor id, bool _]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => _lunaDirty ? id.Value : _luna[(int)id].Vector;
    }

    /// <summary> Get the value for the given color ID as a vector of 4 floats. </summary>
    public Vector4 this[TColorId id, bool _]
    {
        [MethodImpl(ImSharpConfiguration.Inl)]
        get => _customDirty ? _customStorage.GetColor(id).ToVector() : _colors[ToIndex(id)].Vector;
    }

    /// <summary> Get the value for the given ImGui color as a vector of 4 floats. </summary>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public Vector4 AsVector(ImGuiColor id)
        => this[id, true];

    /// <summary> Get the value for the given ImNodes color as a vector of 4 floats. </summary>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public Vector4 AsVector(ImNodesColor id)
        => this[id, true];

    /// <summary> Get the value for the given Dalamud color as a vector of 4 floats. </summary>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public Vector4 AsVector(DalamudColor id)
        => this[id, true];

    /// <summary> Get the value for the given Luna color as a vector of 4 floats. </summary>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public Vector4 AsVector(LunaColor id)
        => this[id, true];

    /// <summary> Get the value for the given color ID as a vector of 4 floats. </summary>
    [MethodImpl(ImSharpConfiguration.Inl)]
    public Vector4 AsVector(TColorId id)
        => this[id, true];

    /// <summary> Create a new color cache based on the given custom color ID and its storage. </summary>
    /// <param name="customStorage"> The storage for custom colors. </param>
    public ColorCache(ColorDictionary<TColorId, TColorData> customStorage)
    {
        _customStorage                     =  customStorage;
        _customStorage.Change              += SetCustomDirty;
        ImSharpDalamudContext.StyleChanged += SetStyleDirty;
        ImSharpPerFrame.Update             += Update;
        Update();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _customStorage.Change              -= SetCustomDirty;
        ImSharpDalamudContext.StyleChanged -= SetStyleDirty;
        ImSharpPerFrame.Update             -= Update;
    }

    /// <summary> Update all cached values. </summary>
    private void Update()
    {
        if (!_anyDirty)
            return;

        if (_imguiDirty && Im.Context.Initialized)
        {
            _imguiDirty = false;
            foreach (var color in ImGuiColor.Values)
                _imgui[(int)color] = color.Get();
        }

        if (_imNodesDirty && ImNodes.Initialized)
        {
            _imNodesDirty = false;
            foreach (var color in ImNodesColor.Values)
            {
                var value = ImNodes.Style[color];
                _imNodes[(int)color] = value.ToVector();
            }
        }

        _dalamudDirty = false;
        foreach (var color in DalamudColor.Values)
        {
            var value = color.Value;
            _dalamud[(int)color] = (value, value);
        }

        _lunaDirty = false;
        foreach (var color in LunaColor.Values)
        {
            var value = color.Value;
            _luna[(int)color] = (value, value);
        }

        if (_customDirty && Im.Context.Initialized && ImNodes.Initialized)
        {
            _customDirty = false;
            foreach (var color in EnumExtensions.get_Values<TColorId>())
            {
                var value = _customStorage.GetColor(color);
                _colors[ToIndex(color)] = (value.ToVector(), value);
            }
        }

        _anyDirty = _imguiDirty || _imNodesDirty || _customDirty;
    }

    private void SetCustomDirty()
        => _anyDirty = _customDirty = true;

    private void SetStyleDirty()
        => _anyDirty = _dalamudDirty = _imNodesDirty = _imguiDirty;

    [MethodImpl(ImSharpConfiguration.OptInl)]
    private static unsafe int ToIndex(TColorId id)
        => *(int*)&id;

    private static T[] GetInitialized<T>(int length, in T value)
    {
        var ret = new T[length];
        Array.Fill(ret, value);
        return ret;
    }
}
