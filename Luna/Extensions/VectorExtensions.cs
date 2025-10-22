namespace Luna;

/// <summary> Extension methods for vectors. </summary>
public static class VectorExtensions
{
    /// <summary> If <paramref name="vector"/> does not fit within <paramref name="maximum"/>, return the largest vector of the same aspect ratio that does. </summary>
    /// <param name="vector"> The vector to contain. </param>
    /// <param name="maximum"> The vector to be contained in. Should be strictly positive. </param>
    /// <returns></returns>
    public static Vector2 Contain(this Vector2 vector, Vector2 maximum)
    {
        if (vector.X > maximum.X)
            vector = maximum with { Y = vector.Y * maximum.X / vector.X };
        if (vector.Y > maximum.Y)
            vector = maximum with { X = vector.X * maximum.Y / vector.Y };

        return vector;
    }
}
