namespace Luna;

public static partial class LunaStyle
{
    /// <summary> A Yes/No column that uses FontAwesome icons instead of custom rendering for the checkmark and cross. </summary>
    /// <typeparam name="TCacheItem"> The type of the checkmark. </typeparam>
    public abstract class YesNoColumn<TCacheItem> : ImSharp.Table.YesNoColumn<TCacheItem>
    {
        /// <summary> Whether the icons should be drawn aligned to the frame padding or not. </summary>
        protected bool FrameAligned { get; init; } = true;

        public override void DrawColumn(in TCacheItem item, int globalIndex)
        {
            if (FrameAligned)
                Im.Cursor.FrameAlign();
            using (AwesomeIcon.Font.Push())
            {
                if (GetValue(item, globalIndex, 0))
                {
                    using var color = ImGuiColor.Text.Push(YesColor);
                    ImEx.TextCentered(TrueIcon.Span);
                }
                else
                {
                    using var color = ImGuiColor.Text.Push(NoColor);
                    ImEx.TextCentered(FalseIcon.Span);
                }
            }

            if (Im.Item.Hovered(HoveredFlags.AllowWhenDisabled))
                DrawTooltip(item, globalIndex);
        }
    }
}
