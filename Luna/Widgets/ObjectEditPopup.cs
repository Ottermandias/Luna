namespace Luna;

/// <summary> A base class for a popup that draws editing for specific objects. </summary>
public abstract class ObjectEditPopup
{
    /// <summary> The ID of the popup. </summary>
    protected abstract ReadOnlySpan<byte> PopupId { get; }

    /// <summary> The object that is currently referenced. </summary>
    protected object? Current;

    /// <summary> Whether the popup has been opened in this frame. </summary>
    protected bool Opened;

    /// <summary> Whether the content of the popup has been edited since being opened. </summary>
    protected bool Edited;

    /// <summary> Open the popup for a specific object. </summary>
    /// <param name="obj"> The object to open for. </param>
    protected void Open(object? obj)
    {
        Current = obj;
        Opened  = obj is not null;
        Edited  = false;
    }

    /// <summary> Draw the popup. This opens the popup if any <see cref="Open"/> method has been called this frame. </summary>
    public void Draw()
    {
        if (Current is null)
            return;

        if (Opened)
        {
            Opened = false;
            Im.Popup.Open(PopupId);
        }

        using var popup = Im.Popup.Begin(PopupId);
        if (popup)
            DrawInternal();
    }

    /// <summary> Draw the actual content of the popup. </summary>
    protected abstract void DrawInternal();

    /// <summary> A helper for closing the popup. </summary>
    protected void Close()
    {
        Edited = false;
        Im.Popup.CloseCurrent();
    }
}
