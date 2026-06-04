using System.Text.Json;

namespace Luna;

/// <summary>
///   A base utility for parsing conditions.
///   The type parsing and all the basic building block conditions are already handled.
/// </summary>
public static class ConditionParser
{
    /// <typeparam name="TContext"><inheritdoc cref="ICondition{TContext}"/></typeparam>
    public static bool TryParse<TContext>(ref Utf8JsonReader reader, out ICondition<TContext>? condition)
        where TContext : IConditionContext<TContext>
    {
        var obj = reader.CreateObjectLimit();
        condition = null;
        if (reader.TryPeekStringProperty("Type"u8, out var type) is not JsonFunctions.PeekError.Success)
        {
            // Could not find type. Skip to the end of the object.
            while (obj.Read(ref reader))
                ;
            return false;
        }

        var ret = false;
        if (type.Equals("True"u8))
        {
            condition = TrueCondition<TContext>.Instance;
            ret       = true;
        }
        else if (type.Equals("False"u8))
        {
            condition = FalseCondition<TContext>.Instance;
            ret       = true;
        }
        else if (type.Equals("And"u8))
        {
            while (obj.Read(ref reader))
            {
                if (!reader.CheckProperty("Conditions"u8))
                    continue;

                if (reader.TokenType is JsonTokenType.Null)
                {
                    condition = TrueCondition<TContext>.Instance;
                }
                else if (!TryReadArray<TContext>(ref reader, out var array))
                {
                    ret = false;
                }
                else
                {
                    condition = new AndCondition<TContext>(array!);
                    ret       = true;
                }
            }
        }
        else if (type.Equals("Or"u8))
        {
            while (obj.Read(ref reader))
            {
                if (!reader.CheckProperty("Conditions"u8))
                    continue;

                if (reader.TokenType is JsonTokenType.Null)
                {
                    condition = FalseCondition<TContext>.Instance;
                }
                else if (!TryReadArray<TContext>(ref reader, out var array))
                {
                    ret = false;
                }
                else
                {
                    condition = new OrCondition<TContext>(array!).Reduce();
                    ret       = true;
                }
            }
        }
        else if (type.Equals("Not"u8))
        {
            while (obj.Read(ref reader))
            {
                if (!reader.CheckProperty("Condition"u8))
                    continue;

                if (reader.TokenType is not JsonTokenType.StartObject)
                {
                    ret = false;
                }
                else if (TryParse<TContext>(ref reader, out var subCondition))
                {
                    condition = new NotCondition<TContext>(subCondition!).Reduce();
                    ret       = true;
                }
                else
                {
                    ret = false;
                }
            }
        }
        else
        {
            condition = TContext.ParseCustomType(ref reader, obj, type);
            ret       = condition is not null;
        }

        // Skip to the end of the object.
        while (obj.Read(ref reader))
            ;

        return ret;
    }

    /// <summary> Read an array of conditions. <c>null</c> is treated as an empty array. </summary>
    private static bool TryReadArray<TContext>(ref Utf8JsonReader reader, out ICondition<TContext>[]? array)
        where TContext : IConditionContext<TContext>
    {
        if (reader.TokenType is JsonTokenType.Null)
        {
            array = [];
            return true;
        }

        if (reader.TokenType is not JsonTokenType.StartArray)
        {
            array = null;
            return false;
        }

        var list        = new List<ICondition<TContext>>();
        var arrayReader = reader.CreateObjectLimit();
        var failure     = false;
        while (arrayReader.Read(ref reader))
        {
            if (reader.TokenType is JsonTokenType.StartObject && TryParse<TContext>(ref reader, out var subCondition))
            {
                list.Add(subCondition!);
            }
            else
            {
                failure = true;
                break;
            }
        }

        if (failure)
        {
            array = null;
            return false;
        }

        array = list.ToArray();
        return true;
    }
}
