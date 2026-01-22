namespace Luna;

/// <summary> Extension methods for enums. </summary>
public static class EnumExtensions
{
    extension<T>(T) where T : unmanaged, Enum
    {
        /// <summary> Get all values of an enumeration more efficiently than with <see cref="Enum.GetValues"/>. </summary>
        public static IReadOnlyList<T> Values
            => Values<T>.Data;
    }

    private static class Values<T> where T : unmanaged, Enum
    {
        public static readonly T[] Data = Enum.GetValues<T>();
    }
}
