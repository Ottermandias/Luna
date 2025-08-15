using Dalamud.Interface;

namespace Luna;

/// <summary> Extensions to convert or draw <see cref="FontAwesomeIcon"/> enum values. </summary>
public static class AwesomeIconExtensions
{
    /// <summary> Convert a FontAwesomeIcon enum value to a <seealso cref="IIconStandIn"/>. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AwesomeIcon Icon(this FontAwesomeIcon icon)
        => icon;

    /// <inheritdoc cref="ImEx.Icon.Draw{T}(T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Draw(this FontAwesomeIcon icon)
        => ImEx.Icon.Draw(icon.Icon());

    /// <inheritdoc cref="ImEx.Icon.Draw{T}(T,Rgba32)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Draw(this FontAwesomeIcon icon, Rgba32 color)
        => ImEx.Icon.Draw(icon.Icon(), color);

    /// <inheritdoc cref="ImEx.Icon.CalculateSize{T}(T)"/>
    public static Vector2 CalculateSize(this FontAwesomeIcon icon)
        => ImEx.Icon.CalculateSize(icon.Icon());

    /// <inheritdoc cref="ImEx.Icon.Button{T}(T,Vector2,ButtonFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Button(this FontAwesomeIcon icon)
        => ImEx.Icon.Button(icon.Icon());
}
