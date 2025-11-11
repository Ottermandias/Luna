namespace Luna;

/// <summary> Flags that specify behaviors of path objects. </summary>
[Flags]
public enum PathFlags : byte
{
    /// <summary> No specific flags. </summary>
    None = 0,

    /// <summary> The node can not be moved via drag & drop. </summary>
    Locked = 0x01,

    /// <summary> The node is currently expanded. </summary>
    Expanded = 0x02,

    /// <summary> The node is currently selected. </summary>
    Selected = 0x04,
}
