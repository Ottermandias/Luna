namespace Luna;

/// <summary> Bitwise operation utility methods. </summary>
public static class BitFunctions
{
    /// <summary> Remove a single bit, moving all further bits one down. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static uint RemoveBit(uint config, int bit)
    {
        var lowMask  = (1u << bit) - 1u;
        var highMask = ~((1u << (bit + 1)) - 1u);
        var low      = config & lowMask;
        var high     = (config & highMask) >> 1;
        return low | high;
    }

    /// <summary> Remove a single bit, moving all further bits one down. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong RemoveBit(ulong config, int bit)
    {
        var lowMask  = (1ul << bit) - 1ul;
        var highMask = ~((1ul << (bit + 1)) - 1ul);
        var low      = config & lowMask;
        var high     = (config & highMask) >> 1;
        return low | high;
    }

    /// <summary> Move a bit in an uint from its position to another, shifting other bits accordingly. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static uint MoveBit(uint config, int bit1, int bit2)
    {
        var enabled = (config & (1 << bit1)) != 0 ? 1u << bit2 : 0u;
        config = RemoveBit(config, bit1);
        var lowMask = (1u << bit2) - 1u;
        var low     = config & lowMask;
        var high    = (config & ~lowMask) << 1;
        return low | enabled | high;
    }

    /// <summary> Move a bit in an ulong from its position to another, shifting other bits accordingly. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static ulong MoveBit(ulong config, int bit1, int bit2)
    {
        var enabled = (config & (1ul << bit1)) != 0 ? 1ul << bit2 : 0ul;
        config = RemoveBit(config, bit1);
        var lowMask = (1ul << bit2) - 1ul;
        var low     = config & lowMask;
        var high    = (config & ~lowMask) << 1;
        return low | enabled | high;
    }
}
