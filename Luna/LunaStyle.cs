using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Luna.Generators;

namespace Luna;

[NamedEnum]
public enum TestEnum
{
    [Name("ABC")]
    A,
    [Name("DEF")]
    B,
    [Name(Omit: true)]
    C,

    GHI,
}

[AssociatedEnum(typeof(B), ForwardDefaultValue: nameof(B.A), BackwardMethod: "BackToA", BackwardDefaultValue: nameof(A.C))]
[DataEnum(typeof(Type), "MyType", Nullable: true)]
public enum A
{
    [Associate(nameof(B.B))]
    A,
    [Associate(nameof(B.A))]
    B,
    [Associate(Omit: true)]
    C,
    [Data("MyType", "typeof(int)")]
    D,
}

public enum B
{
    A,
    B,
    D,
}

public static class LunaStyle
{
    public static void Test()
    {
        TestEnum.A.ToName();
        A.A.ToB();
        A.A.MyType();
    }

    /// <summary> The icon that should be used for deletion buttons. </summary>
    public static readonly AwesomeIcon DeleteIcon = FontAwesomeIcon.Trash;

    /// <summary> The icon that should be used for help- or tooltip markers, generally with the disabled text color. </summary>
    public static readonly AwesomeIcon HelpMarker = FontAwesomeIcon.InfoCircle;

    /// <summary> The default color for error borders in inputs. </summary>
    public static readonly Rgba32 ErrorBorderColor = 0xFF4040F0;

    /// <summary> Get the icon and color associated with a specific notification type. </summary>
    /// <param name="notification"> The notification type. </param>
    /// <returns> The associated icon and it's default color. </returns>
    public static (AwesomeIcon Icon, Rgba32 Color) GetIcon(this NotificationType notification)
        => notification switch
        {
            NotificationType.Success => (FontAwesomeIcon.CheckCircle, 0xFF40FF40),
            NotificationType.Warning => (FontAwesomeIcon.ExclamationCircle, 0xFF40FFFF),
            NotificationType.Error   => (FontAwesomeIcon.TimesCircle, 0xFF4040FF),
            NotificationType.Info    => (FontAwesomeIcon.QuestionCircle, 0xFFFF4040),
            _                        => (FontAwesomeIcon.None, 0),
        };

    /// <summary> Draw a help marker. </summary>
    /// <param name="color"> The color to use. If null, <see cref="ImGuiColor.TextDisabled"/> will be used.</param>
    /// <returns> True if the help marker is hovered by the mouse cursor in this frame. </returns>
    public static bool DrawHelpMarker(ColorParameter color = default)
    {
        ImEx.Icon.Draw(HelpMarker, color.CheckDefault(ImGuiColor.TextDisabled));
        return Im.Item.Hovered(HoveredFlags.AllowWhenDisabled);
    }

    /// <summary> Draw a help marker aligned to frame padding. </summary>
    /// <inheritdoc cref="DrawHelpMarker"/>
    public static bool DrawAlignedHelpMarker(ColorParameter color = default)
    {
        Im.Cursor.FrameAlign();
        ImEx.Icon.Draw(HelpMarker, color.CheckDefault(ImGuiColor.TextDisabled));
        return Im.Item.Hovered(HoveredFlags.AllowWhenDisabled);
    }
}
