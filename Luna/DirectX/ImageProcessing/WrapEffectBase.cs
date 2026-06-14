using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;

namespace Luna.DirectX;

/// <summary> Base class for effects that process a single input texture as a Dalamud wrap. </summary>
/// <seealso cref="ITextureReadbackProvider"/>
public abstract class WrapEffectBase : IEffect
{
    /// <summary> The image to process. </summary>
    public TextureStandIn Input;

    /// <inheritdoc/>
    public abstract int Count { get; }

    /// <inheritdoc/>
    public abstract ImTextureId this[int index] { get; }

    IList<TextureStandIn> IEffect.Inputs
        => field ??= new InputList(this);

    /// <inheritdoc/>
    public IEnumerable<IEffect> GetDependencies()
    {
        if (Input.TryGetListAndIndex(out var list, out _) && list is IEffect effect)
            yield return effect;
    }

    /// <inheritdoc/>
    public Task Run(CancellationToken cancellationToken)
        => Input.InvokeWithWrap(wrap => Run(wrap, cancellationToken));

    /// <summary> Runs this effect. </summary>
    /// <param name="wrap"> The input texture. </param>
    /// <param name="cancellationToken"> A cancellation token. </param>
    /// <returns> A task that represents this effect running. </returns>
    protected abstract Task Run(IDalamudTextureWrap wrap, CancellationToken cancellationToken);

    private sealed class InputList(WrapEffectBase owner) : IList<TextureStandIn>
    {
        /// <inheritdoc/>
        public int Count
            => 1;

        /// <inheritdoc/>
        public bool IsReadOnly
            => false;

        /// <inheritdoc/>
        public TextureStandIn this[int index]
        {
            get
            {
                ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);
                return owner.Input;
            }
            set
            {
                ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);
                owner.Input = value;
            }
        }

        /// <inheritdoc/>
        public IEnumerator<TextureStandIn> GetEnumerator()
        {
            yield return owner.Input;
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        /// <inheritdoc/>
        public bool Contains(TextureStandIn item)
            => owner.Input == item;

        /// <inheritdoc/>
        public void CopyTo(TextureStandIn[] array, int arrayIndex)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
            if (arrayIndex >= array.Length)
                throw new ArgumentException("The array index is past the length of the array", nameof(arrayIndex));

            array[arrayIndex] = owner.Input;
        }

        /// <inheritdoc/>
        public int IndexOf(TextureStandIn item)
            => item == owner.Input ? 0 : -1;

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
    }
}
