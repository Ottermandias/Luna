using Dalamud.Interface.Textures.TextureWraps;

namespace Luna.DirectX;

/// <summary> Wraps an effect graph in a single effect. Can be used for multi-pass effects such as separable filters. </summary>
/// <param name="graph"> The effect graph to wrap. </param>
public class SubGraphEffect(EffectGraph graph) : IEffect, IDisposable, ITextureWrapProvider
{
    /// <summary> A list of inputs of effects of the wrapped graph, re-exported by this effect. </summary>
    public readonly List<(IEffect Effect, int Index)> InputBindings = new(4);

    /// <summary> A list of outputs of effects of the wrapped graph, re-exported by this effect. </summary>
    public readonly List<TextureStandIn> OutputBindings = new(4);

    /// <summary> The wrapped effect graph. </summary>
    public EffectGraph EffectGraph
        => graph;

    /// <inheritdoc/>
    public int Count
        => OutputBindings.Count;

    /// <inheritdoc/>
    public ImTextureId this[int index]
        => OutputBindings[index];

    IList<TextureStandIn> IEffect.Inputs
        => field ??= new InputList(this);

    ~SubGraphEffect()
        => Dispose(false);

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary> Releases the resources used by this object. </summary>
    /// <param name="disposing"> True if called explicitly, false if garbage collected. </param>
    protected virtual void Dispose(bool disposing)
        => graph.Dispose();

    /// <inheritdoc/>
    public IEnumerable<IEffect> GetDependencies()
        => graph.SelectMany(effect => effect.GetDependencies()).Where(dependency => !graph.Contains(dependency));

    IDalamudTextureWrap? ITextureWrapProvider.GetTextureWrap(int index)
    {
        OutputBindings[index].TryGetWrap(out var wrap);
        return wrap;
    }

    /// <inheritdoc/>
    public Task Run(CancellationToken cancellationToken)
        => graph.Run(null, cancellationToken);

    private sealed class InputList(SubGraphEffect owner) : IList<TextureStandIn>
    {
        /// <inheritdoc/>
        public int Count
            => owner.InputBindings.Count;

        /// <inheritdoc/>
        public bool IsReadOnly
            => false;

        /// <inheritdoc/>
        public TextureStandIn this[int index]
        {
            get => Get(owner.InputBindings[index]);
            set => Set(owner.InputBindings[index], value);
        }

        private IEnumerable<TextureStandIn> AsEnumerable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => owner.InputBindings.Select(Get);
        }

        /// <inheritdoc/>
        public IEnumerator<TextureStandIn> GetEnumerator()
            => AsEnumerable.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        /// <inheritdoc/>
        public bool Contains(TextureStandIn item)
            => AsEnumerable.Contains(item);

        /// <inheritdoc/>
        public void CopyTo(TextureStandIn[] array, int arrayIndex)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
            if (arrayIndex + owner.InputBindings.Count > array.Length)
                throw new ArgumentException("The last item that would be copied is past the length of the array", nameof(arrayIndex));

            foreach (var item in AsEnumerable)
                array[arrayIndex++] = item;
        }

        /// <inheritdoc/>
        public int IndexOf(TextureStandIn item)
            => AsEnumerable.IndexOf(item);

        #region Unsupported operations (this implementation is fixed-size)

        /// <inheritdoc/>
        public void Add(TextureStandIn item)
            => throw new NotSupportedException();

        /// <inheritdoc/>
        public void Clear()
            => throw new NotSupportedException();

        /// <inheritdoc/>
        public void Insert(int index, TextureStandIn item)
            => throw new NotSupportedException();

        /// <inheritdoc/>
        public bool Remove(TextureStandIn item)
            => throw new NotSupportedException();

        /// <inheritdoc/>
        public void RemoveAt(int index)
            => throw new NotSupportedException();

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TextureStandIn Get((IEffect Effect, int Index) binding)
            => binding.Effect.Inputs[binding.Index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Set((IEffect Effect, int Index) binding, TextureStandIn value)
            => binding.Effect.Inputs[binding.Index] = value;
    }
}
