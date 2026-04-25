using System.Reflection;

namespace Luna;

internal static class ResourceProvider
{
    private const string ResourcePrefix = "Luna.Resources.";

    private static Stream GetManifestResourceStream(string name)
        => Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourcePrefix + name)
#if DEBUG
         ?? throw new Exception(
                $"ManifestResource \"{name}\" not found - Available resources: \"{string.Join("\", \"", Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(n => n.StartsWith(ResourcePrefix)).Select(n => n[ResourcePrefix.Length..]))}\"");
#else
        ?? throw new Exception($"ManifestResource \"{name}\" not found");
#endif

    public static byte[] GetManifestResourceBytes(string name)
    {
        using var source = GetManifestResourceStream(name);

        using var buffer = new MemoryStream();
        source.CopyTo(buffer);

        return buffer.ToArray();
    }
}
