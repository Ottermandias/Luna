namespace Luna;

/// <summary> Class to draw and edit a list of tags. </summary>
public static class TagButtons
{
    /// <summary> Draw the list of tags. </summary>
    /// <param name="label"> A text entry displayed before the list and used as ID. The line-broken list is wrapped at the end of this text. Does not have to be null-terminated. </param>
    /// <param name="description"> Optional description displayed when hovering over a help marker before the label (if the description is not empty.) Does not have to be null-terminated. </param>
    /// <param name="tags"> The list of tags. </param>
    /// <param name="editedTag"> If the return value is greater or equal to 0, the user input for the tag given by the index. </param>
    /// <param name="editable"> Controls whether the buttons can be used to edit their tags and if new tags can be added, also controls the background color. </param>
    /// <param name="xOffset"> An optional offset that is added after the tag as the text wrap point. </param>
    /// <param name="rightEndOffset"> An optional offset that is used to limit how far from the right-edge of the screen the final button can be placed. </param>
    /// <returns> -1 if no change took place yet, the index of an edited tag (or the count of <paramref name="tags"/> for an added one) if an edit was finalized. </returns>
    public static int Draw(Utf8LabelHandler label, Utf8TextHandler description, IReadOnlyCollection<string> tags, out string editedTag,
        bool editable = true, float xOffset = 0, float rightEndOffset = 0)
    {
        using var id  = Im.Id.Push(ref label);
        var       ret = -1;

        using var group      = Im.Group();
        var       helpMarker = description.GetSpan(out var d) && d.Length > 0;
        if (helpMarker)
            LunaStyle.DrawAlignedHelpMarker(description);
        if (label.GetSpan(out var l) && l.Length > 0)
        {
            if (helpMarker)
                Im.Line.SameInner();
            ImEx.TextFrameAligned(ref label);
            Im.Line.SameInner();
        }

        var x = Im.Cursor.X + xOffset;
        Im.Cursor.X = x;
        editedTag   = string.Empty;

        var       color = Im.Style[editable ? ImGuiColor.Button : ImGuiColor.FrameBackground];
        using var style = ImStyleDouble.ItemSpacing.PushX(4 * Im.Style.GlobalScale);
        using var c     = ImGuiColor.Button.Push(color);
        if (!editable)
            c.Push(ImGuiColor.ButtonHovered, color)
                .Push(ImGuiColor.ButtonActive, color);
        rightEndOffset += 4 * Im.Style.GlobalScale;
        foreach (var (idx, tag) in tags.Index())
        {
            id.Push(idx);
            if (_editIdx == idx && Im.Id.IsCurrent(_currentButton))
            {
                var width = SetPosText(_currentTag, x);
                SetFocus();
                ret = InputString(width, tag, out editedTag);
            }
            else
            {
                SetPosButton(tag, x, rightEndOffset);
                Button(tag, idx, editable);

                if (editable)
                {
                    var delete = Im.Io.KeyControl && Im.Item.RightClicked();
                    Im.Tooltip.OnHover("Hold control and right-click to delete."u8);
                    if (delete)
                    {
                        editedTag = string.Empty;
                        ret       = idx;
                    }
                }
            }

            Im.Line.Same();
            id.Pop();
        }

        if (!editable)
            return -1;

        if (_editIdx == tags.Count && Im.Id.IsCurrent(_currentButton))
        {
            var width = SetPosText(_currentTag, x);
            SetFocus();
            ret = InputString(width, string.Empty, out editedTag);
        }
        else
        {
            SetPos(Im.Style.FrameHeight, x, rightEndOffset);
            if (!ImEx.Icon.Button(LunaStyle.AddObjectIcon, "Add Tag..."u8))
                return ret;

            _currentButton = Im.Id.Current;
            _editIdx       = tags.Count;
            _setFocus      = true;
            _currentTag    = string.Empty;
        }

        return ret;
    }

    private static void SetFocus()
    {
        if (!_setFocus)
            return;

        Im.Keyboard.SetFocusHere();
        _setFocus = false;
    }

    private static float SetPos(float width, float x, float rightEndOffset = 0)
    {
        if (width + Im.Style.ItemSpacing.X >= Im.ContentRegion.Available.X - rightEndOffset)
        {
            Im.Line.New();
            Im.Cursor.X = x;
        }

        return width;
    }

    private static void SetPosButton(string tag, float x, float rightEndOffset = 0)
        => SetPos(Im.Font.CalculateButtonSize(tag).X, x, rightEndOffset);

    private static float SetPosText(string tag, float x)
        => SetPos(Im.Font.CalculateButtonSize(tag).X + 15 * Im.Style.GlobalScale, x);

    private static int InputString(float width, string oldTag, out string editedTag)
    {
        Im.Item.SetNextWidth(width);
        Im.Input.Text("##edit"u8, ref _currentTag);
        if (Im.Item.Deactivated)
        {
            _currentButton = default;
            editedTag      = _currentTag;
            var ret = editedTag == oldTag ? -1 : _editIdx;
            _editIdx = -1;
            return ret;
        }

        editedTag = string.Empty;
        return -1;
    }

    private static void Button(string tag, int idx, bool editable)
    {
        if (!Im.Button(tag) || !editable)
            return;

        _currentButton = Im.Id.Current;
        _editIdx       = idx;
        _setFocus      = true;
        _currentTag    = tag;
    }

    private static ImGuiId _currentButton;
    private static string  _currentTag = string.Empty;
    private static int     _editIdx    = -1;
    private static bool    _setFocus;
}
