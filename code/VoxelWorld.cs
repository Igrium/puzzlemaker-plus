using Godot;
using System.Collections.Generic;
using System.Linq;

namespace PuzzlemakerPlus;
public class VoxelWorld<T>
{
    private readonly Dictionary<Vector3I, VoxelChunk<T>> chunks = new();

    /// <summary>
    /// A dictionary of all the chunks in this world.
    /// </summary>
    public IDictionary<Vector3I, VoxelChunk<T>> Chunks => chunks;

    public T? Get(int x, int y, int z)
    {
        int chunkX = x >> 4;
        int chunkY = y >> 4;
        int chunkZ = z >> 4;

        int localX = x & 15;
        int localY = y & 15;
        int localZ = z & 15;

        var chunk = chunks.GetValueOrDefault(new Vector3I(localX, localY, localZ));
        if (chunk == null) return default;

        return chunk.Get(localX, localY, localZ);
    }

    public T? Get(Vector3I pos)
    {
        return Get(pos.X, pos.Y, pos.Z);
    }

    public T? Set(int x, int y, int z, T value)
    {
        int chunkX = x >> 4;
        int chunkY = y >> 4;
        int chunkZ = z >> 4;

        int localX = x & 15;
        int localY = y & 15;
        int localZ = z & 15;

        Vector3I chunkPos = new Vector3I(localX, localY, localZ);
        var chunk = chunks.GetValueOrDefault(chunkPos);

        if (chunk == null)
        {
            if (EqualityComparer<T>.Default.Equals(value, default))
                return default;

            chunk = new VoxelChunk<T>();
            chunks[chunkPos] = chunk;
        }

        return chunk.Set(localX, localY, localZ, value);
    }

    public T? Set(Vector3I pos, T value)
    {
        return Set(pos.X, pos.Y, pos.Z, value);
    }

    /// <summary>
    /// Iterate over all the positions in this world that might have a non-default voxel in them.
    /// </summary>
    /// <param name="forceOrdered">If set, positions will be iterated over negative-first</param>
    /// <returns></returns>
    public IEnumerable<(Vector3I, T)> IterateVoxels(bool forceOrdered = false)
    {
        IEnumerable<KeyValuePair<Vector3I, VoxelChunk<T>>> chunks;
        if (forceOrdered)
        {
            var list = Chunks.ToList();
            list.Sort((val1, val2) =>
            {
                if (val1.Key.Z != val2.Key.Z)
                {
                    return val1.Key.Z - val2.Key.Z;
                }
                else if (val1.Key.Y != val2.Key.Y)
                {
                    return val1.Key.Y - val2.Key.Y;
                }
                else
                {
                    return val1.Key.X - val2.Key.X;
                }
            });
            chunks = list;
        }
        else
        {
            chunks = Chunks;
        }

        foreach (var (chunkPos, chunk) in chunks)
        {
            Vector3I chunkStart = chunkPos * 16;
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        Vector3I globalPos = chunkStart + new Vector3I(x, y, z);
                        T val = chunk.Get(x, y, z);
                        yield return (globalPos, val);
                    }
                }
            }
        }

        yield break;
    }

}

/// <summary>
/// A 16x16 array of blocks.
/// </summary>
/// <typeparam name="T">Block type</typeparam>
public class VoxelChunk<T>
{
    private readonly T[] data = new T[16 * 16 * 16];

    /// <summary>
    /// The flat array containing the chunk's data.
    /// </summary>
    public T[] Data { get => data; }

    /// <summary>
    /// Get the value at a specific locaiton.
    /// </summary>
    /// <param name="x">Local X coordinate.</param>
    /// <param name="y">Local Y coordinate.</param>
    /// <param name="z">Local Z coordinate.</param>
    /// <returns>The value.</returns>
    public T Get(int x, int y, int z)
    {
        int index = x + (y * 16) + (z * 16 * 16);
        return data[index];
    }

    /// <summary>
    /// Set the value at a specific location.
    /// </summary>
    /// <param name="x">Local X coordinate.</param>
    /// <param name="y">Local Y coordinate.</param>
    /// <param name="z">Local Z coordinate.</param>
    /// <param name="value">New value.</param>
    /// <returns>The previous value.</returns>
    public T Set(int x, int y, int z, T value)
    {
        int index = x + (y * 16) + (z * 16 * 16);
        T prev = data[index];
        data[index] = value;
        return prev;
    }
}

internal static class Vector3IExtensions
{
    public static int SumComponents(this Vector3I vec)
    {
        return vec.X + vec.Y + vec.Z;
    }
}