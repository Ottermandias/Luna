using System.Text.Json;

namespace Luna;

/// <summary>
///   A base class for parsing conditions that can be extended to handle the actual context-based conditions.
///   The type parsing and all the basic building block conditions are already handled.
/// </summary>
/// <typeparam name="TContext"><inheritdoc cref="ICondition{TContext}"/></typeparam>
public class ConditionParser<TContext>
{
    public bool TryParse(ref Utf8JsonReader reader, out ICondition<TContext>? condition)
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
                else if (!TryReadArray(ref reader, out var array))
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
                else if (!TryReadArray(ref reader, out var array))
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
                else if (TryParse(ref reader, out var subCondition))
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
            condition = ParseCustomType(ref reader, obj, type);
            ret       = condition is not null;
        }

        // Skip to the end of the object.
        while (obj.Read(ref reader))
            ;

        return ret;
    }

    /// <summary> Handle all custom condition types for this context. </summary>
    /// <param name="reader"> The reader, currently placed either at the start of an object or after the 'Type'-property, if it was the first in the object. </param>
    /// <param name="obj"> The object reader that can be used to stay within the current JSON object. </param>
    /// <param name="type"> The value of the 'Type'-property. </param>
    /// <returns> A non-null object when a custom condition could be parsed and created successfully, null otherwise. </returns>
    protected virtual ICondition<TContext>? ParseCustomType(ref Utf8JsonReader reader, Utf8JsonObjectLimit obj, StringU8 type)
        => null;

    /// <summary> Read an array of conditions. <c>null</c> is treated as an empty array. </summary>
    private bool TryReadArray(ref Utf8JsonReader reader, out ICondition<TContext>[]? array)
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
            if (reader.TokenType is JsonTokenType.StartObject && TryParse(ref reader, out var subCondition))
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
