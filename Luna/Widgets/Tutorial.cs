namespace Luna;

/// <summary> A base class for tutorials with multiple steps that can be shown in sequence. </summary>
public class Tutorial
{
    /// <summary> A single step in a tutorial. </summary>
    /// <param name="Name"> The header text for the step. </param>
    /// <param name="Text"> The inner text. </param>
    /// <param name="Enabled"> Whether the text should be shown. </param>
    public record struct Step(StringU8 Name, StringU8 Text, bool Enabled);

    /// <summary> The color used to highlight the object focused by the tutorial. </summary>
    public Vector4 HighlightColor { get; init; } = new Rgba32(0xFF20FFFF).ToVector();

    /// <summary> The border color of the tutorial popup. </summary>
    public Vector4 BorderColor { get; init; } = new Rgba32(0xD00000FF).ToVector();

    /// <summary> The label used for the tutorial popup. </summary>
    public StringU8 PopupLabel { get; init; } = new("Tutorial"u8);

    /// <summary> The list of all steps. </summary>
    private readonly List<Step> _steps = [];

    /// <summary> The additional number of frames to wait before reopening the popup for the next tutorial after closing it. </summary>
    private int _waitFrames;

    /// <summary> The step indicating that all tutorials are done. </summary>
    public int EndStep
        => _steps.Count;

    /// <inheritdoc cref="_steps"/>
    public IReadOnlyList<Step> Steps
        => _steps;

    /// <inheritdoc cref="Register(StringU8,StringU8)"/>
    public Tutorial Register(ReadOnlySpan<byte> name, ReadOnlySpan<byte> text)
        => Register(new StringU8(name), new StringU8(text));

    /// <summary> Register a new tutorial step. </summary>
    /// <param name="name"> The header text for the step. </param>
    /// <param name="text"> The inner text. </param>
    /// <returns> The tutorial itself for chaining calls. </returns>
    public Tutorial Register(StringU8 name, StringU8 text)
    {
        _steps.Add(new Step(new StringU8(name), new StringU8(text), true));
        return this;
    }

    /// <summary> Register a deprecated tutorial step that is always disabled. </summary>
    /// <remarks> If your tutorial structure changes, but you do not want to migrate the current step, you can use those.</remarks>
    public Tutorial Deprecated()
    {
        _steps.Add(new Step(StringU8.Empty, StringU8.Empty, false));
        return this;
    }

    /// <summary> Open the tutorial popup if the current step matches the given id. </summary>
    /// <param name="id"> The ID of the queried step. </param>
    /// <param name="current"> The current step. </param>
    /// <param name="setter"> The function to update the current step after the popup is closed. </param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Open(int id, int current, Action<int> setter)
    {
        if (current != id)
            return;

        OpenWhenMatch(current, setter);
        --_waitFrames;
    }

    /// <summary> Skip a tutorial popup if the current step matches the given id. </summary>
    /// <param name="id"> The ID of the queried step. </param>
    /// <param name="current"> The current step. </param>
    /// <param name="setter"> The function to update the current step after the skip. </param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Skip(int id, int current, Action<int> setter)
    {
        if (current != id)
            return;

        setter(NextId(current));
    }

    /// <summary> Open the appropriate tutorial popup, and update the current step. </summary>
    /// <param name="current"></param>
    /// <param name="setter"></param>
    private void OpenWhenMatch(int current, Action<int> setter)
    {
        var step = Steps[current];

        // Skip disabled tutorials.
        if (!step.Enabled)
        {
            setter(NextId(current));
            return;
        }

        if (_waitFrames > 0)
            --_waitFrames;
        else if (Im.Window.Focused(FocusedFlags.RootAndChildWindows) && !Im.Popup.IsOpen(PopupLabel))
            Im.Popup.Open(PopupLabel);

        var windowPos = HighlightObject();
        DrawPopup(windowPos, step, NextId(current), setter);
    }

    /// <summary> Highlight the object referenced by the popup with a colored border around its item rectangle. </summary>
    private Vector2 HighlightObject()
    {
        Im.Scroll.SetHereX();
        Im.Scroll.SetHereY();
        var       offset = ImEx.ScaledVector(5, 4);
        var       min    = Im.Item.UpperLeftCorner - offset;
        var       max    = Im.Item.LowerRightCorner + offset;
        using var rect   = Im.DrawList.Foreground.PushClipRect(Rectangle.FromSize(Im.Window.Position - offset, Im.Window.Size + 2 * offset));
        Im.DrawList.Foreground.Shape.Rectangle(min, max, HighlightColor, 5 * Im.Style.GlobalScale, ImDrawFlagsRectangle.RoundCornersAll,
            2 * Im.Style.GlobalScale);
        return max + new Vector2(Im.Style.GlobalScale);
    }

    /// <summary> Draw the actual tutorial popup. </summary>
    /// <param name="pos"> The position to draw the popup at. </param>
    /// <param name="step"> The data for the drawn step. </param>
    /// <param name="next"> The index of the next step to jump to. </param>
    /// <param name="setter"> The function to update the current step. </param>
    private void DrawPopup(Vector2 pos, Step step, int next, Action<int> setter)
    {
        using var style = Im.Style.PushDefault()
            .Push(ImStyleBorder.Popup,         BorderColor, 2 * Im.Style.GlobalScale)
            .Push(ImStyleSingle.PopupRounding, 5 * Im.Style.GlobalScale)
            .Push(ImGuiColor.PopupBackground,  Vector4.UnitW);
        using var font = Im.Font.PushDefault();

        // Prevent the window from opening outside the screen.
        var size = new Vector2(350 * Im.Style.GlobalScale, 0);
        var diff = Im.Window.Width - size.X;
        pos.X = diff < 0 ? Im.Window.Position.X : Math.Clamp(pos.X, Im.Window.Position.X, Im.Window.Position.X + diff);

        // Ensure the header line is visible with a button to go to next.
        pos.Y = Math.Clamp(pos.Y, Im.Window.Position.Y + Im.Style.FrameHeightWithSpacing,
            Im.Window.Position.Y + Im.Window.Height - Im.Style.FrameHeightWithSpacing);

        Im.Window.SetNextPosition(pos);
        Im.Window.SetNextSize(size);
        Im.Window.FocusNext();
        using var popup = Im.Popup.Begin(PopupLabel, WindowFlags.AlwaysAutoResize | WindowFlags.Popup);
        if (!popup)
            return;

        ImEx.TextFrameAligned(step.Name);
        Im.Line.Same(Im.ContentRegion.Available.X - Im.Style.TextHeight);
        int? nextValue = ImEx.Icon.Button(LunaStyle.NextIcon, "Go to next tutorial step."u8)
            ? next
            : null;

        Im.Separator();
        using (Im.PushTextWrapPosition())
        {
            var span = step.Text.Span;
            foreach (var text in span.Split((byte)'\n'))
            {
                var line = span[text].Trim();
                if (line.Length is 0)
                    Im.Line.New();
                else
                    Im.Text(line);
            }
        }

        Im.Line.New();
        var buttonText = next == EndStep ? "Finish"u8 : "Next"u8;
        nextValue = Im.Button(buttonText) ? next : nextValue;
        Im.Line.Same();
        nextValue = Im.Button("Skip Tutorial"u8) ? EndStep : nextValue;
        Im.Tooltip.OnHover("Skip all current tutorial entries, but show any new ones added later."u8);
        Im.Line.Same();
        nextValue = Im.Button("Disable Tutorial"u8) ? -1 : nextValue;
        Im.Tooltip.OnHover("Disable all tutorial entries."u8);

        if (nextValue != null)
        {
            setter(nextValue.Value);
            _waitFrames = 2;
            Im.Popup.CloseCurrent();
        }
    }

    /// <summary> Obtain the ID of the next enabled step after the current one, if any.</summary>
    private int NextId(int current)
    {
        for (var i = current + 1; i < EndStep; ++i)
        {
            if (Steps[i].Enabled)
                return i;
        }

        return EndStep;
    }

    /// <summary> Obtain the current ID if it is enabled, and otherwise the first enabled id after it. </summary>
    public int CurrentEnabledId(int current)
    {
        if (current < 0)
            return -1;

        for (var i = current; i < EndStep; ++i)
        {
            if (Steps[i].Enabled)
                return i;
        }

        return EndStep;
    }

    /// <summary> Make sure you have as many tutorials registered as you intend to. </summary>
    public Tutorial EnsureSize(int size)
    {
        if (_steps.Count != size)
            throw new Exception("Tutorial size is incorrect.");

        return this;
    }
}
