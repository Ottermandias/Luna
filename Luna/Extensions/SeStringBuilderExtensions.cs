using Dalamud.Game.Text.SeStringHandling;
using Lumina.Excel.Sheets;

namespace Luna;

/// <summary> Extensions for the string builder class to use specific colors more easily. </summary>
public static class SeStringBuilderExtensions
{
    /// <summary> The default green color used for text, see <see cref="UIColor"/>. </summary>
    public const ushort Green = 504;

    /// <summary> The default yellow color used for text, see <see cref="UIColor"/>. </summary>
    public const ushort Yellow = 31;

    /// <summary> The default red color used for text, see <see cref="UIColor"/>. </summary>
    public const ushort Red = 534;

    /// <summary> The default blue color used for text, see <see cref="UIColor"/>. </summary>
    public const ushort Blue = 517;

    /// <summary> The default white color used for text, see <see cref="UIColor"/>. </summary>
    public const ushort White = 1;

    /// <summary> The default purple color used for text, see <see cref="UIColor"/>. </summary>
    public const ushort Purple = 541;

    /// <summary> Add text in a specific color, optionally within [] brackets to the string builder. </summary>
    /// <param name="sb"> The SeStringBuilder used. </param>
    /// <param name="text"> The text to write. </param>
    /// <param name="color"> The color to use, as defined by <see cref="UIColor"/>. </param>
    /// <param name="brackets"> Whether to put the text within [] brackets. </param>
    /// <returns> The input SeStringBuilder for method chaining. </returns>
    public static SeStringBuilder AddText(this SeStringBuilder sb, string text, int color, bool brackets = false)
        => sb.AddUiForeground((ushort)color).AddText(brackets ? $"[{text}]" : text).AddUiForegroundOff();

    /// <summary> Add green text, optionally within [] brackets to the string builder. </summary>
    /// <inheritdoc cref="AddText"/>
    public static SeStringBuilder AddGreen(this SeStringBuilder sb, string text, bool brackets = false)
        => sb.AddText(text, Green, brackets);

    /// <summary> Add yellow text, optionally within [] brackets to the string builder. </summary>
    /// <inheritdoc cref="AddText"/>
    public static SeStringBuilder AddYellow(this SeStringBuilder sb, string text, bool brackets = false)
        => sb.AddText(text, Yellow, brackets);

    /// <summary> Add red text, optionally within [] brackets to the string builder. </summary>
    /// <inheritdoc cref="AddText"/>
    public static SeStringBuilder AddRed(this SeStringBuilder sb, string text, bool brackets = false)
        => sb.AddText(text, Red, brackets);

    /// <summary> Add blue text, optionally within [] brackets to the string builder. </summary>
    /// <inheritdoc cref="AddText"/>
    public static SeStringBuilder AddBlue(this SeStringBuilder sb, string text, bool brackets = false)
        => sb.AddText(text, Blue, brackets);

    /// <summary> Add white text, optionally within [] brackets to the string builder. </summary>
    /// <inheritdoc cref="AddText"/>
    public static SeStringBuilder AddWhite(this SeStringBuilder sb, string text, bool brackets = false)
        => sb.AddText(text, White, brackets);

    /// <summary> Add purple text, optionally within [] brackets to the string builder. </summary>
    /// <inheritdoc cref="AddText"/>
    public static SeStringBuilder AddPurple(this SeStringBuilder sb, string text, bool brackets = false)
        => sb.AddText(text, Purple, brackets);

    /// <summary> Add a command with description following a list indentation marker to the string builder. </summary>
    /// <param name="sb"> The SeStringBuilder used. </param>
    /// <param name="command"> The command name to add. </param>
    /// <param name="description"> The description of the command to add. </param>
    /// <returns> The input SeStringBuilder for method chaining. </returns>
    public static SeStringBuilder AddCommand(this SeStringBuilder sb, string command, string description)
        => sb.AddText("    》 ")
            .AddBlue(command)
            .AddText($" - {description}");

    /// <summary> Add a word with its first symbol highlighted in purple, optionally appending a comma and a space. </summary>
    /// <param name="sb"> The SeStringBuilder used. </param>
    /// <param name="word"> The word to add. </param>
    /// <param name="withComma"> whether to append a comma and space or not. </param>
    /// <returns> The input SeStringBuilder for method chaining. </returns>
    public static SeStringBuilder AddInitialPurple(this SeStringBuilder sb, string word, bool withComma = true)
        => sb.AddPurple($"[{word[0]}]")
            .AddText(withComma ? $"{word[1..]}, " : word[1..]);
}
