namespace Luna;

/// <summary>   A base class for drawing conditions that can be extended to handle the actual context-based conditions. </summary>
/// <typeparam name="TContext"><inheritdoc cref="ICondition{TContext}"/></typeparam>
public class ConditionDrawer<TContext>
    where TContext : IConditionContext<TContext>
{
    /// <summary> A stack that lists AND/OR/NOT notes as parents. </summary>
    protected readonly Stack<ICondition<TContext>> ParentStack = [];

    /// <summary> Draw a condition. </summary>
    public bool Draw(ICondition<TContext>? condition, TContext? context, out ICondition<TContext>? replace)
        => condition switch
        {
            TrueCondition<TContext>  => DrawConstantCondition(true,  out replace),
            FalseCondition<TContext> => DrawConstantCondition(false, out replace),
            NotCondition<TContext> n => DrawNotCondition(n, context, out replace),
            AndCondition<TContext> a => DrawSetCondition(a, "And"u8, context, out replace),
            OrCondition<TContext> o  => DrawSetCondition(o, "Or"u8,  context, out replace),
            _                        => DrawCustom(condition, context, out replace),
        };

    protected virtual bool DrawCustom(ICondition<TContext>? condition, TContext? context, out ICondition<TContext>? replace)
    {
        replace = null;
        return false;
    }

    protected virtual bool DrawNotCondition(NotCondition<TContext> condition, TContext? context, out ICondition<TContext>? replace)
    {
        using var group = Im.Group();
        replace = null;
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

        replace = null;
        return false;
    }

    /// <summary> Draw a set of conditions grouped by And or Or. </summary>
    protected virtual bool DrawSetCondition(IList<ICondition<TContext>> conditions, ReadOnlySpan<byte> type, TContext? context,
        out ICondition<TContext>? replace)
    {
        using var group = Im.Group();
        var       size  = Im.Font.CalculateButtonSize("And"u8);
        Im.Cursor.X += size.X + Im.Style.ItemInnerSpacing.X;
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
            using var id = Im.Id.Push(i);
            // Draw each additional condition with a set button.
            if (Im.Button(type, size))
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

            if (!Draw(conditions[i], context, out subReplace))
                continue;

            ret = true;
            if (subReplace is null)
                conditions.RemoveAt(i--);
            else
                conditions[i] = subReplace.Reduce();
        }

        ParentStack.Pop();

        if (!ret)
        {
            replace = null;
            return false;
        }

        if (conditions.Count is 0)
        {
            replace = null;
            return true;
        }

        if (conditions.Count is 1)
        {
            replace = conditions[0].Reduce();
            return true;
        }

        replace = ((ICondition<TContext>)conditions).Reduce();
        return true;
    }
}
