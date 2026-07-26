namespace Luna;

/// <remarks> Not yet implemented. </remarks>
public enum LunaColor
{ }

public static class LunaColorExtensions
{
    extension(LunaColor id)
    {
        /// <summary> Get the currently assigned value for a Luna color. </summary>
        /// <remarks> Not yet implemented. </remarks>
        public Vector4 Value
        {
            [MethodImpl(ImSharpConfiguration.OptInl)]
            get => id switch
            {
                _ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
            };
        }
    }
}
