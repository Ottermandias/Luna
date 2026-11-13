namespace Luna;

/// <summary> Extension methods for versions. </summary>
public static class VersionExtensions
{
    extension(Version version)
    {
        /// <summary> Check whether a version equals specific values. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(int major, int minor)
            => version.Major == major && version.Minor == minor;

        /// <inheritdoc cref="Equals(Version, int, int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(int major, int minor, int build)
            => version.Major == major && version.Minor == minor && version.Build == build;

        /// <inheritdoc cref="Equals(Version, int, int)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(int major, int minor, int build, int revision)
            => version.Major == major && version.Minor == minor && version.Build == build && version.Revision == revision;
    }
}
