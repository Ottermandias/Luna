using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;

namespace Luna;

public static partial class LunaStyle
{
    /// <summary> The icon that should be used for generic save buttons. </summary>
    public static readonly AwesomeIcon SaveIcon = FontAwesomeIcon.Save;

    /// <summary> The icon that should be used for deletion buttons. </summary>
    public static readonly AwesomeIcon DeleteIcon = FontAwesomeIcon.Trash;

    /// <summary> The icon that should be used for help- or tooltip markers, generally with the disabled text color. </summary>
    public static readonly AwesomeIcon HelpMarker = FontAwesomeIcon.InfoCircle;

    /// <summary> The icon that should be used for buttons to add new objects to a list. </summary>
    public static readonly AwesomeIcon AddObjectIcon = FontAwesomeIcon.Plus;

    /// <summary> The icon that should be used for buttons to add folders to a filesystem. </summary>
    public static readonly AwesomeIcon AddFolderIcon = FontAwesomeIcon.FolderPlus;

    /// <summary> The icon that should be used for incognito toggle checkboxes when the incognito state is currently on. </summary>
    public static readonly AwesomeIcon IncognitoOn = FontAwesomeIcon.EyeSlash;

    /// <summary> The icon that should be used for incognito toggle checkboxes when the incognito state is currently off. </summary>
    public static readonly AwesomeIcon IncognitoOff = FontAwesomeIcon.Eye;

    /// <summary> The icon that should be used to show a tagging system or preconfigured tags. </summary>
    public static readonly AwesomeIcon TagsMarker = FontAwesomeIcon.Tags;

    /// <summary> The icon that should be used to show favorites. </summary>
    public static readonly AwesomeIcon FavoriteIcon = FontAwesomeIcon.Star;

    /// <summary> The icon that should be used to show buttons opening folders. </summary>
    public static readonly AwesomeIcon FolderIcon = FontAwesomeIcon.Folder;

    /// <summary> The icon that should be used to show reloading or refreshing. </summary>
    public static readonly AwesomeIcon RefreshIcon = FontAwesomeIcon.Recycle;

    /// <summary> The icon that should be used for buttons that expand something that is currently collapsed downwards. </summary>
    public static readonly AwesomeIcon ExpandDownIcon = FontAwesomeIcon.CaretDown;

    /// <summary> The icon that should be used for buttons that collapse something that is currently expanded downwards. </summary>
    public static readonly AwesomeIcon CollapseUpIcon = FontAwesomeIcon.CaretUp;

    /// <summary> The icon that should be used for buttons that expand something that is currently collapsed rightwards. </summary>
    public static readonly AwesomeIcon ExpandRightIcon = FontAwesomeIcon.CaretRight;

    /// <summary> The icon that should be used for buttons that collapse something that is currently expanded rightwards. </summary>
    public static readonly AwesomeIcon CollapseLeftIcon = FontAwesomeIcon.CaretLeft;

    /// <summary> The icon that should be used for buttons that export to files. </summary>
    public static readonly AwesomeIcon FileExportIcon = FontAwesomeIcon.FileExport;

    /// <summary> The icon that should be used for things that are locked or write-protected. </summary>
    public static readonly AwesomeIcon LockedIcon = FontAwesomeIcon.Lock;

    /// <summary> The icon that should be used for things copying data to the user's clipboard. </summary>
    public static readonly AwesomeIcon ToClipboardIcon = FontAwesomeIcon.Clipboard;

    /// <summary> The icon that should be used for things applying data from the user's clipboard. </summary>
    public static readonly AwesomeIcon FromClipboardIcon = FontAwesomeIcon.Paste;

    /// <summary> The icon that should be used for importing files or text. </summary>
    public static readonly AwesomeIcon ImportIcon = FontAwesomeIcon.FileImport;

    /// <summary> The icon that should be used for things that are unlocked. </summary>
    public static readonly AwesomeIcon UnlockedIcon = FontAwesomeIcon.LockOpen;

    /// <summary> The icon that should be used for buttons that move to the next object. </summary>
    public static readonly AwesomeIcon NextIcon = FontAwesomeIcon.ArrowCircleRight;

    /// <summary> The icon that should be used for buttons that open further editing for an object. </summary>
    public static readonly AwesomeIcon EditIcon = FontAwesomeIcon.Edit;

    /// <summary> The icon that should be used for buttons that open an object in an external editor. </summary>
    public static readonly AwesomeIcon OpenExternalIcon = FontAwesomeIcon.FileExport;

    /// <summary> The icon that should be used for buttons that interact when being hovered. </summary>
    public static readonly AwesomeIcon OnHoverIcon = FontAwesomeIcon.Crosshairs;

    /// <summary> The icon that should be used for applying colors to things. </summary>
    public static readonly AwesomeIcon DyeIcon = FontAwesomeIcon.PaintBrush;

    /// <summary> The default color for error borders in inputs. </summary>
    public static readonly Vector4 ErrorBorderColor = new Rgba32(0xFF4040F0).ToVector();

    /// <summary> The color for activated favorites icons. </summary>
    public static readonly Vector4 FavoriteColor = new(1, 1, 0, 1);

    /// <summary> The color for discord. </summary>
    public static readonly Vector4 DiscordColor = new(0.45f, 0.55f, 0.85f, 1);

    /// <summary> The default color for the ReniGuide. </summary>
    public static readonly Vector4 ReniColorButton = new(0.55f, 0.40f, 0.80f, 1);

    /// <summary> The hovered color for the ReniGuide. </summary>
    public static readonly Vector4 ReniColorHovered = new(0.69f, 0.44f, 0.69f, 1);

    /// <summary> The active color for the ReniGuide. </summary>
    public static readonly Vector4 ReniColorActive = new(0.88f, 0.44f, 0.56f, 1);

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

    /// <summary> Draw a full GUID in mono font that can be copied on click. </summary>
    /// <param name="id"> The GUID to draw. </param>
    public static void DrawGuid(Guid id)
    {
        Span<byte> span = stackalloc byte[37];
        span[^1] = 0;
        if (!id.TryFormat(span, out var count) || count is not 36)
            return;

        using (Im.Font.PushMono())
        {
            if (Im.Selectable(span))
            {
                try
                {
                    Im.Clipboard.Set(span);
                }
                catch
                {
                    // ignored
                }
            }
        }

        Im.Tooltip.OnHover("Click to copy to clipboard."u8);
    }
}
