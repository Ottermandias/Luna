namespace Luna;

/// <summary>   A base class for drawing conditions that can be extended to handle the actual context-based conditions. </summary>
/// <typeparam name="TContext"><inheritdoc cref="ICondition{TContext}"/></typeparam>
public class ConditionDrawer<TContext>
    where TContext : IConditionContext<TContext>
{
    /// <summary> A stack that lists AND/OR/NOT notes as parents. </summary>
    protected readonly Stack<ICondition<TContext>> ParentStack = [];

    /// <summary> A selected new condition to add. </summary>
    protected ICondition<TContext>? NewCondition;

    /// <summary> The current width required for an And button. </summary>
    protected Vector2 AndButtonSize;

    /// <summary> The current width required for a No button. </summary>
    protected Vector2 NotButtonSize;

    /// <summary> The color of the line. </summary>
    protected Rgba32 LineColor;

    /// <summary> The width of the connector line. </summary>
    protected float LineWidth = 3;

    /// <summary> When the sizes were updated last. </summary>
    private int _frameUpdate = -1;

    /// <summary> Draw a condition. If this returns true and <paramref name="replace"/> is null, it should be removed. </summary>
    public bool Draw(ICondition<TContext>? condition, TContext? context, out ICondition<TContext>? replace)
    {
        UpdateSizes();
        using var id = Im.Id.Push(ParentStack.Count);
        var ret = condition switch
        {
            TrueCondition<TContext>  => DrawConstantCondition(true,  out replace),
            FalseCondition<TContext> => DrawConstantCondition(false, out replace),
            NotCondition<TContext> n => DrawNotCondition(n, context, out replace),
            AndCondition<TContext> a => DrawSetCondition(a, "And"u8, context, out replace),
            OrCondition<TContext> o  => DrawSetCondition(o, "Or"u8,  context, out replace),
            _                        => DrawCustom(condition, context, out replace),
        };
        if (condition is null or ListCondition<TContext> || ParentStack.TryPeek(out var c) && c is AndCondition<TContext>)
            return ret;

        id.Push(-1);
        if (ParentStack.Count > 0)
        {
            var size = Im.Font.CalculateButtonSize("And"u8).X;
            Im.Cursor.X += (size + Im.Style.ItemInnerSpacing.X) * ParentStack.Count;
        }

        if (Draw(null, context, out var newCondition) && newCondition is not null)
        {
            ret     = true;
            replace = new AndCondition<TContext>(condition, newCondition);
        }

        return ret;
    }

    /// <summary> Draw the button to add new conditions. </summary>
    /// <param name="replaced"> The newly added condition when the button is pressed. </param>
    /// <returns> True on button click.</returns>
    protected virtual bool DrawAddConditionButton(ref ICondition<TContext>? replaced)
    {
        if (!ImEx.Icon.Button(LunaStyle.AddObjectIcon, "Add a new condition."u8, NewCondition is null))
            return false;

        replaced     = NewCondition;
        NewCondition = null;
        return true;
    }

    /// <summary> Draw the button to delete a specific condition. </summary>
    /// <param name="replaced"> The condition to delete. </param>
    /// <returns> True on click, in which case <paramref name="replaced"/> is set to null. </returns>
    protected virtual bool DrawDeleteConditionButton(ref ICondition<TContext>? replaced)
    {
        if (!ImEx.Icon.Button(LunaStyle.DeleteIcon, "Remove this condition."u8))
            return false;

        replaced = null;
        return true;
    }

    protected virtual bool DrawCustom(ICondition<TContext>? condition, TContext? context, out ICondition<TContext>? replace)
    {
        replace = condition;
        return false;
    }

    protected virtual bool DrawNotCondition(NotCondition<TContext> condition, TContext? context, out ICondition<TContext>? replace)
    {
        using var group = Im.Group();
        replace = condition;
        var ret = false;
        using (ImGuiColor.Button.Push(LunaStyle.WarningForeground))
        {
            if (Im.Button("Not"u8))
            {
                replace = condition.Condition.Reduce();
                ret     = true;
            }
        }

        Im.Line.SameInner();
        ParentStack.Push(condition);
        if (Draw(condition.Condition, context, out var innerReplace))
        {
            ret = true;
            if (innerReplace is null)
            {
                replace = null;
            }
            else
            {
                condition.Condition = innerReplace;
                replace             = condition.Reduce();
            }
        }

        ParentStack.Pop();

        return ret;
    }

    /// <summary> Draw a constant condition. </summary>
    protected virtual bool DrawConstantCondition(bool value, out ICondition<TContext>? replace)
    {
        // Switch the constant value.
        if (Im.Button(value ? "True"u8 : "False"u8))
        {
            replace = value ? FalseCondition<TContext>.Instance : TrueCondition<TContext>.Instance;
            return true;
        }

        Im.Line.SameInner();
        // Remove the constant value.
        if (ImEx.Icon.Button(LunaStyle.DeleteIcon))
        {
            replace = null;
            return true;
        }

        replace = value ? TrueCondition<TContext>.Instance : FalseCondition<TContext>.Instance;
        return false;
    }

    protected void DrawTopLine(Im.DrawList.DrawListPath drawList, Vector2 screenPos)
    {
        var center = screenPos + new Vector2((AndButtonSize.X - LineWidth) / 2, Im.Style.FrameHeight / 2);
        drawList.LineTo(center with { Y = screenPos.Y + Im.Style.FrameHeight + Im.Style.ItemInnerSpacing.Y })
            .LineTo(center)
            .LineTo(center with { X = screenPos.X + AndButtonSize.X + Im.Style.ItemInnerSpacing.X })
            .FinishStroke(LineColor, ImDrawFlagsPath.None, LineWidth);
    }

    /// <summary> Draw a set of conditions grouped by And or Or. </summary>
    protected virtual bool DrawSetCondition(IList<ICondition<TContext>> conditions, ReadOnlySpan<byte> type, TContext? context,
        out ICondition<TContext>? replace)
    {
        using var id        = new Im.IdDisposable();
        using var group     = Im.Group();
        var       screenPos = Im.Cursor.ScreenPosition;
        Im.Cursor.X += AndButtonSize.X + Im.Style.ItemInnerSpacing.X;
        var i   = 1;
        var ret = false;
        ParentStack.Push((ICondition<TContext>)conditions);

        // Draw the first condition without a set button.
        if (Draw(conditions[0], context, out var subReplace))
        {
            ret = true;
            if (subReplace is null)
            {
                conditions.RemoveAt(0);
                --i;
            }
            else
            {
                conditions[0] = subReplace.Reduce();
            }
        }

        for (; i < conditions.Count; ++i)
        {
            id.Push(i);
            // Draw each additional condition with a set button.
            if (Im.Button(type, AndButtonSize))
            {
                // Split this entry and the prior one off into a subgroup.
                var prior   = conditions[i - 1];
                var current = conditions[i];
                conditions.RemoveAt(i--);
                var newGroup = conditions is AndCondition<TContext>
                    ? new OrCondition<TContext>(prior, current).Reduce()
                    : new AndCondition<TContext>(prior, current).Reduce();
                conditions[i] = newGroup;
                ret           = true;
            }

            var start = Im.Item.LowerRightCorner;
            start.X -= (Im.Item.Size.X + LineWidth) / 2;

            Im.Line.SameInner();
            if (Draw(conditions[i], context, out subReplace))
            {
                ret = true;
                if (subReplace is null)
                    conditions.RemoveAt(i--);
                else
                    conditions[i] = subReplace.Reduce();
            }

            var color = i == conditions.Count - 1 ? LineColor : LineColor.WithAlpha(Im.Style.DisabledAlpha);
            Im.Window.DrawList.Shape.Line(start, start with { Y = Im.Cursor.ScreenY }, color, LineWidth);
        }

        id.Push(-1);
        ImEx.Button(type, AndButtonSize, true);
        Im.Line.SameInner();
        if (Draw(null, context, out var newCondition) && newCondition is not null)
        {
            ret = true;
            conditions.Add(newCondition);
        }

        ParentStack.Pop();

        if (!ret)
        {
            replace = null;
            return false;
        }

        replace = ((ICondition<TContext>)conditions).Reduce();
        return true;
    }

    /// <summary> Update the sizes only once per frame. </summary>
    /// <returns> True if an update took place. </returns>
    protected virtual bool UpdateSizes()
    {
        if (_frameUpdate == Im.State.FrameCount)
            return false;

        _frameUpdate  = Im.State.FrameCount;
        AndButtonSize = Im.Font.CalculateButtonSize("And"u8);
        NotButtonSize = Im.Font.CalculateButtonSize("Not"u8);
        LineColor     = ImGuiColor.Button.Get();
        LineWidth     = 3 * Im.Style.GlobalScale;
        return true;
    }
}
