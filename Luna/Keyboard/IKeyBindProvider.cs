namespace Luna;

/// <summary> A provider for miscellaneous shared keybinds. </summary>
public interface IKeyBindProvider
{
    /// <summary> The modifier used for anything that can not be easily undone or restored, like deletions. </summary>
    public DoubleModifier SecurityModifier { get; }

    /// <summary> The modifier used for anything that is not critical but should not be done by accident, like toggling the incognito mode. </summary> </summary>
    public DoubleModifier MisclickModifier { get; }
}
