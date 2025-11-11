namespace Luna;

/// <summary> A popup to handle user input. </summary>
public static class InputPopup
{
    /// <summary> Open a new single line text input popup for names and move keyboard focus to it. </summary>
    /// <inheritdoc cref="Open"/>
    public static bool OpenName(Utf8LabelHandler popupName, out string result, float width = 0)
        => Open(popupName, StringU8.Empty, out result, "Enter New Name..."u8, width);

    /// <summary> Open a new single line text input popup with a hint and move keyboard focus to it. </summary>
    /// <param name="popupName"> The name of the popup to begin. This needs to be on the same id stack level as the <see cref="Im.Popup.Open(Utf8LabelHandler,PopupFlags)"/> call. </param>
    /// <param name="inputText"> The input text. If this is changed while the widget is active this will not be reflected. </param>
    /// <param name="result"> The entered text when this returns true, otherwise empty. </param>
    /// <param name="hint"> The hint to draw into the text input when <paramref name="inputText"/> is empty. </param>
    /// <param name="width"> The width for the text input in pixels. If this is non-positive, <c>300 * <see cref="Im.ImGuiStyle.GlobalScale">GlobalScale</see></c> is used. </param>
    /// <returns> True if the input was confirmed by pressing Enter this frame, false otherwise. </returns>
    public static bool Open(Utf8LabelHandler popupName, Utf8TextHandler inputText, out string result, Utf8HintHandler hint, float width = 0)
    {
        using var popup = Im.Popup.Begin(popupName);
        result = string.Empty;
        if (!popup)
            return false;

        if (Im.Keyboard.IsPressed(Key.Escape))
            Im.Popup.CloseCurrent();

        if (width <= 0)
            width = 300 * Im.Style.GlobalScale;
        Im.Item.SetNextWidth(width);
        if (Im.Window.Appearing)
            Im.Keyboard.SetFocusHere();

        if (!ImEx.InputOnDeactivation.Text("##input"u8, inputText, out result, hint))
            return false;

        Im.Popup.CloseCurrent();
        return true;
    }
}
