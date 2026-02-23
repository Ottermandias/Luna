namespace Luna;

/// <summary> A utility to more easily handle predefined tags for tagging objects. </summary>
/// <typeparam name="TProvider"> The file name provider for the predefined tags file. </typeparam>
/// <typeparam name="TObj"> The objects that are being tagged. </typeparam>
/// <param name="saveService"> A save service for saving the predefined tags. </param>
/// <param name="messager"> A messager for error reporting. </param>
public abstract class PredefinedTagManager<TProvider, TObj>(BaseSaveService<TProvider> saveService, MessageService messager)
    : ConfigurationFile<TProvider>(saveService, messager), IReadOnlyList<string>
    where TProvider : BaseFilePathProvider
    where TObj : class
{
    /// <summary> Whether the list of predefined tags is currently set to be open. </summary>
    public bool IsListOpen { get; private set; }

    /// <summary> The color for tag buttons that can be added to the object. </summary>
    public virtual Vector4 AddButtonColor
        => LunaStyle.AddPredefinedTagColor;

    /// <summary> The color for tag buttons that will be removed from the object. </summary>
    public virtual Vector4 RemoveButtonColor
        => LunaStyle.RemovePredefinedTagColor;

    /// <summary> Whether this tag manager supports two types of tags (e.g. local and global, or user and creator), or just one. </summary>
    public virtual bool HasGlobalTags
        => false;

    /// <summary> The name of the type of object being tagged, used for display and error messages. </summary>
    public virtual string ObjectName
        => "object";

    /// <summary> The category that local tags represent for the object (e.g. local or user). </summary>
    public virtual string LocalTagName
        => "local tag";

    /// <summary> The category that global tags represent for the object (e.g. global or creator). </summary>
    public virtual string GlobalTagName
        => "global tag";

    /// <summary> The actual tags as a sorted list. </summary>
    protected readonly SortedListAdapter<string> PredefinedTags = new([], StringComparer.InvariantCultureIgnoreCase);

    private Vector4 _addButtonColor;
    private Vector4 _removeButtonColor;

    /// <inheritdoc/>
    public override int CurrentVersion
        => 2;

    /// <summary> Whether there are any tags defined. </summary>
    public bool Enabled
        => PredefinedTags.Count > 0;

    /// <inheritdoc/>
    protected override void AddData(JsonTextWriter j)
    {
        j.WritePropertyName("Tags");
        j.WriteStartArray();
        foreach (var tag in PredefinedTags)
            j.WriteValue(tag);

        j.WriteEndArray();
    }

    /// <inheritdoc/>
    protected override void LoadData(JObject j)
    {
        if (j["Tags"] is JArray array)
            foreach (var tag in array)
            {
                if (tag.ToObject<string>() is not { } value)
                    Messager.NotificationMessage("Non-string tag found, ignoring.");
                else if (!PredefinedTags.AddUnique(value))
                    Messager.NotificationMessage($"Duplicate tag {tag} found in predefined tags, ignoring.");
            }
    }

    /// <inheritdoc/>
    public IEnumerator<string> GetEnumerator()
        => PredefinedTags.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => PredefinedTags.Count;

    /// <inheritdoc/>
    public string this[int index]
        => PredefinedTags[index];

    /// <summary> Change a tag in the predefined tags. </summary>
    /// <param name="tagIdx"> The index of the tag to change. If this is the same as the current count, a new tag is added. </param>
    /// <param name="tag"> The new value to change it to. If this is empty, the tag is removed. </param>
    public void ChangeSharedTag(int tagIdx, string tag)
    {
        if (tagIdx < 0 || tagIdx > PredefinedTags.Count)
            return;

        if (tagIdx != PredefinedTags.Count)
            PredefinedTags.RemoveAt(tagIdx);

        if (!string.IsNullOrEmpty(tag))
            PredefinedTags.AddUnique(tag);

        Save();
    }

    /// <summary> Draw a button to toggle the visibility of the predefined tags list.  </summary>
    public void DrawToggleButton()
    {
        using var color = ImGuiColor.Button.Push(Im.Style[ImGuiColor.ButtonActive], IsListOpen);
        if (ImEx.Icon.Button(LunaStyle.TagsMarker, "Add Predefined Tags..."u8))
            IsListOpen = !IsListOpen;
    }

    /// <summary> Draw a button to toggle the visibility of the predefined tags list in the top right corner.  </summary>
    private void DrawToggleButtonTopRight()
    {
        var scrollBar = Im.Scroll.MaximumY > 0 ? Im.Style.ItemInnerSpacing.X : 0;
        Im.Line.Same(Im.ContentRegion.Maximum.X - Im.Style.FrameHeight - scrollBar);
        DrawToggleButton();
    }

    /// <summary> Draw a basic editor list for predefined tags, including the visibility button in the top right corner. </summary>
    /// <param name="obj"> The object whose tags are being edited. </param>
    /// <param name="editLocal"> Whether to edit local or global tags. </param>
    public void DrawAddFromSharedTagsAndUpdateTags(TObj obj, bool editLocal)
    {
        DrawToggleButtonTopRight();
        if (HasGlobalTags)
        {
            if (!DrawList(GetLocalTags(obj), GetGlobalTags(obj), editLocal, out var changedTag, out var index))
                return;

            if (editLocal)
                ChangeLocalTag(obj, index, changedTag);
            else
                ChangeGlobalTag(obj, index, changedTag);
        }
        else if (!editLocal && DrawList(GetLocalTags(obj), out var changedTag, out var index))
        {
            ChangeLocalTag(obj, index, changedTag);
        }
    }

    /// <summary> Draw the list of predefined tags for an object that only has one type of tags. </summary>
    /// <param name="existingTags"> The existing tags. </param>
    /// <param name="changedTag"> The new value of the changed tag. </param>
    /// <param name="changedIndex"> The index of the changed tag, or the current count for a new tag. </param>
    /// <returns> True if any tag was changed this frame. </returns>
    protected virtual bool DrawList(IReadOnlyCollection<string> existingTags, out string changedTag, out int changedIndex)
    {
        changedTag   = string.Empty;
        changedIndex = -1;

        if (!IsListOpen)
            return false;

        var ret = false;
        Im.Text("Predefined Tags"u8);
        Im.Separator();
        _addButtonColor    = AddButtonColor;
        _removeButtonColor = RemoveButtonColor;
        foreach (var (idx, tag) in PredefinedTags.Index())
        {
            var tagIdx = existingTags.IndexOf(tag);
            if (DrawColoredButton(tag, idx, tagIdx, false))
            {
                (changedTag, changedIndex) = tagIdx >= 0 ? (string.Empty, tagIdx) : (tag, existingTags.Count);
                ret                        = true;
            }

            Im.Line.Same();
        }

        Im.Line.New();
        Im.Separator();
        return ret;
    }

    /// <summary> Draw the list of predefined tags for an object that has both types of tags. </summary>
    /// <param name="localTags"> The existing local tags. </param>
    /// <param name="globalTags"> The existing global tags. </param>
    /// <param name="editLocal"> Whether to edit the local or global tags of the object. </param>
    /// <param name="changedTag"> The new value of the changed tag. </param>
    /// <param name="changedIndex"> The index of the changed tag, or the current count for a new tag. </param>
    /// <returns> True if any tag was changed this frame. </returns>
    private bool DrawList(IReadOnlyCollection<string> localTags, IReadOnlyCollection<string> globalTags, bool editLocal, out string changedTag,
        out int changedIndex)
    {
        changedTag   = string.Empty;
        changedIndex = -1;

        if (!IsListOpen)
            return false;

        Im.Text("Predefined Tags"u8);
        Im.Separator();

        _addButtonColor    = AddButtonColor;
        _removeButtonColor = RemoveButtonColor;
        var ret = false;
        var (edited, others) = editLocal ? (localTags, globalTags) : (globalTags, localTags);
        foreach (var (idx, tag) in PredefinedTags.Index())
        {
            var tagIdx  = edited.IndexOf(tag);
            var inOther = tagIdx < 0 && others.IndexOf(tag) >= 0;
            if (DrawColoredButton(tag, idx, tagIdx, inOther))
            {
                (changedTag, changedIndex) = tagIdx >= 0 ? (string.Empty, tagIdx) : (tag, edited.Count);
                ret                        = true;
            }

            Im.Line.Same();
        }

        Im.Line.New();
        Im.Separator();
        return ret;
    }

    private sealed class Cache : BasicCache
    {
        public readonly List<TObj>                       SelectedObjects = [];
        public readonly List<(int Index, int DataIndex)> CountedObjects  = [];

        public void Update(IEnumerable<TObj> selection)
        {
            SelectedObjects.AddRange(selection);
            CountedObjects.EnsureCapacity(SelectedObjects.Count);
            while (CountedObjects.Count < SelectedObjects.Count)
                CountedObjects.Add((-1, -1));
        }

        public override void Update()
            => SelectedObjects.Clear();
    }

    /// <summary> Get the current set of local tags of an object. </summary>
    protected abstract IReadOnlyCollection<string> GetLocalTags(TObj obj);

    /// <summary> Get the current set of global tags of an object. </summary>
    protected virtual IReadOnlyCollection<string> GetGlobalTags(TObj obj)
        => []; // Empty if unsupported.

    /// <summary> Change a local tag of an object. </summary>
    /// <param name="obj"> The object to change. </param>
    /// <param name="tagIndex"> The index of the tag to change. If this is the same as the local count, a new tag is added. </param>
    /// <param name="tag"> The new value of the tag. If this is empty, the tag is removed. </param>
    protected abstract void ChangeLocalTag(TObj obj, int tagIndex, string tag);

    /// <summary> Change a global tag of an object. </summary>
    /// <param name="obj"> The object to change. </param>
    /// <param name="tagIndex"> The index of the tag to change. If this is the same as the local count, a new tag is added. </param>
    /// <param name="tag"> The new value of the tag. If this is empty, the tag is removed. </param>
    protected virtual void ChangeGlobalTag(TObj obj, int tagIndex, string tag)
    {
        // Do nothing if unsupported.
    }

    public void DrawListMulti(IEnumerable<TObj> selection)
    {
        if (!IsListOpen)
            return;

        Im.Text("Predefined Tags"u8);

        using var color = new Im.ColorDisposable();
        var       cache = CacheManager.Instance.GetOrCreateCache(Im.Id.Current, () => new Cache());
        cache.Update(selection);
        _addButtonColor    = AddButtonColor;
        _removeButtonColor = RemoveButtonColor;
        foreach (var (idx, tag) in PredefinedTags.Index())
        {
            var alreadyContained = 0;
            var inModData        = 0;
            var missing          = 0;

            foreach (var (modIndex, mod) in cache.SelectedObjects.Index())
            {
                var tagIdx = GetLocalTags(mod).IndexOf(tag);
                if (tagIdx >= 0)
                {
                    ++alreadyContained;
                    cache.CountedObjects[modIndex] = (tagIdx, -1);
                }
                else if (HasGlobalTags)
                {
                    var dataIdx = GetGlobalTags(mod).IndexOf(tag);
                    if (dataIdx >= 0)
                    {
                        ++inModData;
                        cache.CountedObjects[modIndex] = (-1, dataIdx);
                    }
                    else
                    {
                        ++missing;
                        cache.CountedObjects[modIndex] = (-1, -1);
                    }
                }
            }

            using var id          = Im.Id.Push(idx);
            var       buttonWidth = new Vector2(Im.Font.CalculateButtonSize(tag).X, 0);
            // Prevent adding a new tag past the right edge of the popup
            if (buttonWidth.X + Im.Style.ItemSpacing.X >= Im.ContentRegion.Available.X)
                Im.Line.New();

            var (usedColor, disabled, tt) = (missing, alreadyContained) switch
            {
                (> 0, _) => (_addButtonColor, false,
                    new StringU8(
                        $"Add this tag to {missing} {ObjectName}s.{(inModData > 0 ? $" {inModData} {ObjectName}s contain it in their {GlobalTagName}s and are untouched." : string.Empty)}")),
                (_, > 0) => (_removeButtonColor, false,
                    new StringU8(
                        $"Remove this tag from {alreadyContained} {ObjectName}s.{(inModData > 0 ? $" {inModData} {ObjectName}s contain it in their {GlobalTagName}s and are untouched." : string.Empty)}")),
                _ => (_removeButtonColor, true, new StringU8($"This tag is already present in the {GlobalTagName}s of all selected mods.")),
            };
            color.Push(ImGuiColor.Button, usedColor);
            if (ImEx.Button(tag, buttonWidth, tt, disabled))
            {
                if (missing > 0)
                    foreach (var (obj, (localIdx, _)) in cache.SelectedObjects.Zip(cache.CountedObjects))
                    {
                        if (localIdx < 0)
                            ChangeLocalTag(obj, GetLocalTags(obj).Count, tag);
                    }
                else
                    foreach (var (obj, (localIdx, _)) in cache.SelectedObjects.Zip(cache.CountedObjects))
                    {
                        if (localIdx >= 0)
                            ChangeLocalTag(obj, localIdx, string.Empty);
                    }
            }

            Im.Line.Same();

            color.Pop();
        }

        Im.Line.New();
    }

    private bool DrawColoredButton(string buttonLabel, int index, int tagIdx, bool inOther)
    {
        using var id          = Im.Id.Push(index);
        var       buttonWidth = Im.Font.CalculateButtonSize(buttonLabel).X;
        // Prevent adding a new tag past the right edge of the popup
        if (buttonWidth + Im.Style.ItemSpacing.X >= Im.ContentRegion.Available.X)
            Im.Line.New();

        bool ret;
        using (Im.Disabled(inOther))
        {
            using var color = ImGuiColor.Button.Push(tagIdx >= 0 || inOther ? _removeButtonColor : _addButtonColor);
            ret = Im.Button(buttonLabel);
        }

        if (inOther)
            Im.Tooltip.OnHover(HoveredFlags.AllowWhenDisabled, "This tag is already present in the other set of tags."u8);

        return ret;
    }
}
