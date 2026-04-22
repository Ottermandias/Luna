namespace Luna;

/// <summary> A button that draws a sub menu entry into a menu. </summary>
public sealed class SubMenuButton(StringU8 label) : BaseButton
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label
        => label;

    /// <summary> The button entries for the sub menu. </summary>
    public readonly ButtonList Entries = new();

    /// <inheritdoc/>
    public override bool DrawMenuItem()
    {
        using var sub = Im.Menu.Begin(label);
        if (!sub)
            return false;

        var ret = false;
        foreach (var entry in Entries)
            ret |= entry.DrawMenuItem();

        return ret;
    }
}

/// <summary> A button that draws a sub menu entry into a menu. </summary>
public sealed class SubMenuButton<T>(StringU8 label) : BaseButton<T>
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label(in T data)
        => label;

    /// <summary> The button entries for the sub menu. </summary>
    public readonly ButtonList<T> Entries = new();

    /// <inheritdoc/>
    public override bool DrawMenuItem(in T data)
    {
        using var sub = Im.Menu.Begin(label);
        if (!sub)
            return false;

        var ret = false;
        foreach (var entry in Entries)
            ret |= entry.DrawMenuItem(data);

        return ret;
    }
}
