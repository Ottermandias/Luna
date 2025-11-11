namespace Luna;

/// <summary> A button that draws a separator into a menu. </summary>
public sealed class MenuSeparator : BaseButton
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label
        => StringU8.Empty;

    /// <inheritdoc/>
    public override bool DrawMenuItem()
        => DrawSeparator();

    /// <summary> Draw a separator with some spacing. </summary>
    /// <returns> False. </returns>
    public static bool DrawSeparator()
    {
        Im.Cursor.Y += Im.Style.ItemInnerSpacing.Y;
        Im.Separator();
        Im.Cursor.Y += Im.Style.ItemInnerSpacing.Y;
        return false;
    }
}

/// <inheritdoc cref="MenuSeparator"/>
/// <typeparam name="T"> Ignored type parameter. </typeparam>
public sealed class MenuSeparator<T> : BaseButton<T>
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label(in T _)
        => StringU8.Empty;

    /// <inheritdoc/>
    public override bool DrawMenuItem(in T folder)
        => MenuSeparator.DrawSeparator();
}
