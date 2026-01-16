using System.Security.Cryptography;

namespace Luna.Generators;

internal struct HashCode
{
    private static readonly uint Seed = CreateSeed();

    /// <summary> Primes taken from Microsoft.Bcl.HashCode for compatibility </summary>
    private const uint Prime1 = 2654435761U;

    private const uint Prime2 = 2246822519U;
    private const uint Prime3 = 3266489917U;
    private const uint Prime4 = 668265263U;
    private const uint Prime5 = 374761393U;

    public static int Combine(int hash1, int hash2)
    {
        var hash = Seed + Prime5 + 8;
        hash = Queue(hash, (uint)hash1);
        hash = Queue(hash, (uint)hash2);
        return (int)Finalize(hash);
    }

    private static uint Queue(uint hash, uint value)
        => RotateLeft(hash + value * Prime3, 17) * Prime4;

    private static uint RotateLeft(uint value, int offset)
        => (value << offset) | (value >> (32 - offset));

    private static uint Finalize(uint hash)
    {
        hash ^= hash >> 15;
        hash *= Prime2;
        hash ^= hash >> 13;
        hash *= Prime3;
        hash ^= hash >> 16;
        return hash;
    }

    private static unsafe uint CreateSeed()
    {
        using var rng   = RandomNumberGenerator.Create();
        var       bytes = new byte[sizeof(uint)];
        rng.GetBytes(bytes);
        fixed (byte* ptr = bytes)
        {
            return *(uint*)ptr;
        }
    }
}
