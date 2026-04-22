namespace Luna;

/// <summary> General helper methods for common operations. </summary>
public static partial class LunaHelpers
{
    /// <summary> Set a value only if it is different from the current value, and return whether the value was set. </summary>
    /// <typeparam name="T"> The type of the field. </typeparam>
    /// <param name="field"> The field to change. </param>
    /// <param name="value"> The value to set. </param>
    /// <returns> True if the field's value was changed. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool SetDifferent<T>(ref T field, in T value) where T : IEquatable<T>
    {
        if (field.Equals(value))
            return false;

        field = value;
        return true;
    }
}
