using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiNotification;

namespace Luna;

public static partial class LunaStyle
{
    /// <summary> Modifiers used across Luna widgets. </summary>
    public static readonly ModifierHelper Modifier = new();

    /// <summary> The icon that should be used for generic save buttons. </summary>
    public static readonly AwesomeIcon SaveIcon = FontAwesomeIcon.Save;

    /// <summary> The icon that should be used for deletion buttons. </summary>
    public static readonly AwesomeIcon DeleteIcon = FontAwesomeIcon.Trash;

    /// <summary> The icon that should be used for buttons that back something up and then delete it. </summary>
    public static readonly AwesomeIcon BackupDeleteIcon = FontAwesomeIcon.TrashRestore;

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

    /// <summary> The checkmark icon that should be used to denote a true or active value. </summary>
    public static readonly AwesomeIcon TrueIcon = FontAwesomeIcon.Check;

    /// <summary> The cross icon that should be used to denote a false or inactive value. </summary>
    public static readonly AwesomeIcon FalseIcon = FontAwesomeIcon.Times;

    /// <summary> The icon that should be used to show buttons opening folders. </summary>
    public static readonly AwesomeIcon FolderIcon = FontAwesomeIcon.Folder;

    /// <summary> The icon that should be used when files are removed, but not deleted from something. </summary>
    public static readonly AwesomeIcon RemoveFileIcon = FontAwesomeIcon.FileCircleMinus;

    /// <summary> The icon that should be used when folders are removed, but not deleted from something. </summary>
    public static readonly AwesomeIcon RemoveFolderIcon = FontAwesomeIcon.FolderMinus;

    /// <summary> The icon that should be used to show reloading or refreshing. </summary>
    public static readonly AwesomeIcon RefreshIcon = FontAwesomeIcon.Recycle;

    /// <summary> The icon that should be used to show reloading or refreshing. </summary>
    public static readonly AwesomeIcon TestIcon = FontAwesomeIcon.Flask;

    /// <summary> The icon that should be used for buttons that expand something that behaves like a tree node. </summary>
    public static readonly AwesomeIcon TreeExpandIcon = FontAwesomeIcon.CaretRight;

    /// <summary> The icon that should be used for buttons that collapse something that behaves like a tree node. </summary>
    public static readonly AwesomeIcon TreeCollapseIcon = FontAwesomeIcon.CaretDown;

    /// <summary> The icon that should be used for buttons that expand something that is currently collapsed rightwards. </summary>
    public static readonly AwesomeIcon ExpandRightIcon = FontAwesomeIcon.CaretRight;

    /// <summary> The icon that should be used for buttons that collapse something that is currently expanded rightwards. </summary>
    public static readonly AwesomeIcon CollapseLeftIcon = FontAwesomeIcon.CaretLeft;

    /// <summary> The icon that should be used for pinning things. </summary>
    public static readonly AwesomeIcon PinIcon = FontAwesomeIcon.Thumbtack;

    /// <summary> The icon that should be used for buttons that duplicate data. </summary>
    public static readonly AwesomeIcon DuplicateIcon = FontAwesomeIcon.Clone;

    /// <summary> The icon that should be used for buttons that export to files. </summary>
    public static readonly AwesomeIcon FileExportIcon = FontAwesomeIcon.FileExport;

    /// <summary> The icon that should be used for things that are locked or write-protected. </summary>
    public static readonly AwesomeIcon LockedIcon = FontAwesomeIcon.Lock;

    /// <summary> The icon that should be used for canceling running actions. </summary>
    public static readonly AwesomeIcon CancelIcon = FontAwesomeIcon.Ban;

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

    /// <summary> The icon that should be used for errors. </summary>
    public static readonly AwesomeIcon ErrorIcon = FontAwesomeIcon.TimesCircle;

    /// <summary> The icon that should be used for warnings. </summary>
    public static readonly AwesomeIcon WarningIcon = FontAwesomeIcon.ExclamationCircle;

    /// <summary> The icon that should be used for information or help. </summary>
    public static readonly AwesomeIcon InfoIcon = FontAwesomeIcon.QuestionCircle;

    /// <summary> The icon that should be used for Undo or Revert buttons. </summary>
    public static readonly AwesomeIcon UndoIcon = FontAwesomeIcon.UndoAlt;

    /// <summary> The icon that should be used for Reset buttons. </summary>
    public static readonly AwesomeIcon ResetIcon = FontAwesomeIcon.SyncAlt;

    /// <summary> The icon that should be used to pop a panel out to its own window. </summary>
    public static readonly AwesomeIcon PopOutIcon = FontAwesomeIcon.SquareArrowUpRight;

    /// <summary> The icon that should be used to resize something to its default values or auto-fit values. </summary>
    public static readonly AwesomeIcon AutoResizeIcon = FontAwesomeIcon.Expand;

    /// <summary> The icon that should be used to resize something to its default values or auto-fit values. </summary>
    public static readonly AwesomeIcon CompressIcon = FontAwesomeIcon.Compress;

    /// <summary> The icon that should be used to add horizontal separators. </summary>
    public static readonly AwesomeIcon AddSeparatorIcon = FontAwesomeIcon.XmarksLines;

    /// <summary> The icon that should be used for buttons that enable or disable items in bulk. </summary>
    public static readonly AwesomeIcon ToggleBulkIcon = FontAwesomeIcon.CheckSquare;

    /// <summary> The color for info texts, icons or borders. </summary>
    public static Vector4 InfoForeground
        => ImGuiColors.InfoForeground;

    /// <summary> The color for success texts, icons or borders. </summary>
    public static Vector4 SuccessForeground
        => ImGuiColors.SuccessForeground;

    /// <summary> The color for warning texts, icons or borders. </summary>
    public static Vector4 WarningForeground
        => ImGuiColors.WarningForeground;

    /// <summary> The color for error texts, icons or borders. </summary>
    public static Vector4 ErrorForeground
        => ImGuiColors.ErrorForeground;

    /// <summary> The color for texts, icons or borders that require attention. </summary>
    public static Vector4 AttentionForeground
        => ImGuiColors.AttentionForeground;

    /// <summary> The color for info button or frame backgrounds. </summary>
    public static Vector4 InfoBackground
        => ImGuiColors.InfoBackground;

    /// <summary> The color for success button or frame backgrounds. </summary>
    public static Vector4 SuccessBackground
        => ImGuiColors.SuccessBackground;

    /// <summary> The color for warning button or frame backgrounds. </summary>
    public static Vector4 WarningBackground
        => ImGuiColors.WarningForeground;

    /// <summary> The color for error button or frame backgrounds. </summary>
    public static Vector4 ErrorBackground
        => ImGuiColors.ErrorBackground;

    /// <summary> The color for button or frame backgrounds that require attention. </summary>
    public static Vector4 AttentionBackground
        => ImGuiColors.AttentionBackground;

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

    /// <summary> The default color for tag buttons that can be added to the object. </summary>
    public static readonly Vector4 AddPredefinedTagColor = new Rgba32(0xFF44AA44).ToVector();

    /// <summary> The default color for tag buttons that will be removed from the object. </summary>
    public static readonly Vector4 RemovePredefinedTagColor = new Rgba32(0xFF2222AA).ToVector();

    /// <summary> Get the icon and color associated with a specific notification type. </summary>
    /// <param name="notification"> The notification type. </param>
    /// <returns> The associated icon and it's default color. </returns>
    public static (AwesomeIcon Icon, Vector4 Color) GetIcon(this NotificationType notification)
        => notification switch
        {
            NotificationType.Success => (FontAwesomeIcon.CheckCircle, SuccessForeground),
            NotificationType.Warning => (WarningIcon, WarningForeground),
            NotificationType.Error   => (ErrorIcon, ErrorForeground),
            NotificationType.Info    => (FontAwesomeIcon.QuestionCircle, InfoForeground),
            _                        => (FontAwesomeIcon.None, Vector4.Zero),
        };

    /// <summary> Draw a separator with inner spacing above and below. </summary>
    public static void DrawSeparator()
    {
        Im.Cursor.Y += Im.Style.ItemInnerSpacing.Y;
        Im.Separator();
        Im.Cursor.Y += Im.Style.ItemInnerSpacing.Y;
    }

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
