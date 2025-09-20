namespace Luna;

public class FileSystemSelectorFooter<TCacheItem> : IFooter
{
    protected readonly ButtonList ButtonList = new();

    public bool Collapsed
        => ButtonList.Count is 0;

    public FileSystemSelectorFooter(BaseFileSystem fileSystem)
    {
        ButtonList.AddButton(new FolderAddButton(fileSystem), 50);
    }

    public void Draw(Vector2 size)
    {
        Debug.Assert(ButtonList.Count > 0);
        using var style = Im.Style.Push(ImStyleSingle.FrameRounding, 0);
        size.X /= ButtonList.Count;
        ButtonList[0].DrawButton(size);
        foreach (var button in ButtonList.Skip(1))
        {
            Im.Line.NoSpacing();
            button.DrawButton(size);
        }
    }

    protected sealed class FolderAddButton(BaseFileSystem fileSystem) : BaseIconButton<AwesomeIcon>
    {
        private string _newName = string.Empty;

        private static ReadOnlySpan<byte> PopupId
            => "FolderNamePopup"u8;

        public override ReadOnlySpan<byte> Label
        {
            [MethodImpl(ImSharpConfiguration.Inl)]
            get => "AddFolder"u8;
        }

        public override AwesomeIcon Icon
        {
            [MethodImpl(ImSharpConfiguration.Inl)]
            get => LunaStyle.DeleteIcon;
        }

        public override void DrawTooltip()
            => Im.Text("Create a new, empty folder. Can contain '/' to create a directory structure."u8);

        [MethodImpl(ImSharpConfiguration.Inl)]
        public override void OnClick()
            => Im.Popup.Open(PopupId);

        [MethodImpl(ImSharpConfiguration.Inl)]
        public override void PreDraw()
        {
            // Draw the popup.
            if (!InputPopup.OpenName(PopupId, ref _newName))
                return;

            IFileSystemFolder? folder;
            try
            {
                folder   = fileSystem.FindOrCreateAllFolders(_newName);
                _newName = string.Empty;
            }
            catch
            {
                folder = null;
            }

            if (folder is not null)
            {
                // TODO: expand ancestors.
            }
        }
    }
}
