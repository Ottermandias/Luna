namespace Luna;

/// <summary> A popup to handle user input. </summary>
public static class InputPopup
{
    /// <summary> Open a new single line text input popup for names and move keyboard focus to it. </summary>
    /// <inheritdoc cref="Open"/>
    public static bool OpenName(Utf8LabelHandler popupName, ref string text, float width = 0)
        => Open(popupName, ref text, "Enter New Name..."u8, width);

    /// <summary> Open a new single line text input popup with a hint and move keyboard focus to it. </summary>
    /// <param name="popupName"> The name of the popup to begin. This needs to be on the same id stack level as the <see cref="Im.Popup.Open(Utf8LabelHandler,PopupFlags)"/> call. </param>
    /// <param name="text"> The input/output text. </param>
    /// <param name="hint"> The hint to draw into the text input when <paramref name="text"/> is empty. </param>
    /// <param name="width"> The width for the text input in pixels. If this is non-positive, <c>300 * <see cref="Im.ImGuiStyle.GlobalScale">GlobalScale</see></c> is used. </param>
    /// <returns> True if the input was confirmed by pressing Enter this frame, false otherwise. </returns>
    public static bool Open(Utf8LabelHandler popupName, ref string text, Utf8HintHandler hint, float width = 0)
    {
        using var popup = Im.Popup.Begin(popupName);
        if (!popup)
            return false;

        if (Im.Keyboard.IsPressed(Key.Escape))
        {
            text = string.Empty;
            Im.Popup.CloseCurrent();
        }

        if (width <= 0)
            width = 300 * Im.Style.GlobalScale;
        Im.Item.SetNextWidth(width);
        if (Im.Window.Appearing)
            Im.Keyboard.SetFocusHere();

        if (!Im.Input.Text("##input"u8, ref text, hint, InputTextFlags.EnterReturnsTrue))
            return false;

        Im.Popup.CloseCurrent();
        return true;
    }
}
