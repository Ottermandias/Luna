namespace Luna;

/// <summary>   A base class for drawing conditions that can be extended to handle the actual context-based conditions. </summary>
/// <typeparam name="TContext"><inheritdoc cref="ICondition{TContext}"/></typeparam>
public class ConditionDrawer<TContext>
{
    /// <summary> Draw a condition. </summary>
    public void Draw(ICondition<TContext> condition, TContext? context)
    {
        switch (condition)
        {
            case TrueCondition<TContext>:  DrawConstantCondition(true); break;
            case FalseCondition<TContext>: DrawConstantCondition(false); break;
            case NotCondition<TContext> n:

                using (Im.Group())
                {
                    using (ImGuiColor.Button.Push(LunaStyle.WarningForeground))
                    {
                        ImEx.Button("Not"u8);
                    }

                    Im.Line.SameInner();
                    Draw(n.Condition, context);
                }

                break;
            case AndCondition<TContext> a: DrawSetCondition(a, "And"u8, context); break;
            case OrCondition<TContext> o:  DrawSetCondition(o, "Or"u8,  context); break;
            default:                       DrawCustom(condition, context); break;
        }
    }

    protected virtual void DrawCustom(ICondition<TContext> condition, TContext? context)
    { }

    /// <summary> Draw a constant condition. </summary>
    protected virtual void DrawConstantCondition(bool value)
        => ImEx.TextFramed(value ? "True"u8 : "False"u8);

    /// <summary> Draw a set of conditions grouped by And or Or. </summary>
    protected virtual void DrawSetCondition(IReadOnlyList<ICondition<TContext>> conditions, ReadOnlySpan<byte> type, TContext? context)
    {
        using (Im.Group())
        {
            var size = Im.Font.CalculateButtonSize("And"u8);
            Im.Cursor.X += size.X + Im.Style.ItemInnerSpacing.X;
            Draw(conditions[0], context);
            for (var i = 1; i < conditions.Count; ++i)
            {
                using var id = Im.Id.Push(i);
                Im.Button(type, size);
                Draw(conditions[i], context);
            }
        }
    }
}
