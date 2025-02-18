using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PuzzlemakerPlus;

/// <summary>
/// Called to update the value in a voxel without a bunch of indirection.
/// This doesn't work right now and I can't figure out how to make it work.
/// </summary>
/// <typeparam name="T">Voxel type.</typeparam>
/// <param name="value">A reference to the voxel's value. Update this to update the voxel.</param>
public delegate void VoxelOperator<T>(ref T value);

public partial class VoxelWorld<T> : RefCounted
{

    /// <summary>
    /// Get the chunk position that a certian voxel belongs to.
    /// </summary>
    /// <param name="pos">World position to test.</param>
    /// <returns>Chunk position.</returns>
    public static Vector3I GetChunk(Vector3I pos)
    {
        return new Vector3I(pos.X >> 4, pos.Y >> 4, pos.Z >> 4);
    }

    /// <summary>
    /// Get the local position within a voxel's chunk that a voxel resides at.
    /// </summary>
    /// <param name="pos">Voxel global position.</param>
    /// <returns>Position relative to chunk.</returns>
    public static Vector3I GetPosInChunk(Vector3I pos)
    {
        return new Vector3I(pos.X & 15, pos.Y & 15, pos.Z & 15);
    }

    private readonly Dictionary<Vector3I, VoxelChunk<T>> chunks = new();

    /// <summary>
    /// A dictionary of all the chunks in this world.
    /// </summary>
    public IDictionary<Vector3I, VoxelChunk<T>> Chunks => chunks;

    public T? GetVoxel(int x, int y, int z)
    {
        Vector3I chunkPos = GetChunk(new Vector3I(x, y, z));
        Vector3I localPos = GetPosInChunk(new Vector3I(x, y, z));

        var chunk = chunks.GetValueOrDefault(chunkPos);
        if (chunk == null) return default;

        return chunk.Get(localPos);
    }

    public T? GetVoxel(Vector3I pos)
    {
        return GetVoxel(pos.X, pos.Y, pos.Z);
    }

    public T? SetVoxel(int x, int y, int z, T value)
    {
        int chunkX = x >> 4;
        int chunkY = y >> 4;
        int chunkZ = z >> 4;

        int localX = x & 15;
        int localY = y & 15;
        int localZ = z & 15;

        Vector3I chunkPos = new Vector3I(chunkX, chunkY, chunkZ);
        var chunk = GetOrCreateChunk(chunkPos);
        return chunk.Set(localX, localY, localZ, value);
    }

    public T? SetVoxel(Vector3I pos, T value)
    {
        return SetVoxel(pos.X, pos.Y, pos.Z, value);
    }

    public void UpdateVoxel(int x, int y, int z, VoxelOperator<T> function)
    {
        int chunkX = x >> 4;
        int chunkY = y >> 4;
        int chunkZ = z >> 4;

        int localX = x & 15;
        int localY = y & 15;
        int localZ = z & 15;
        Vector3I chunkPos = new Vector3I(chunkX, chunkY, chunkZ);
        var chunk = GetOrCreateChunk(chunkPos);

        chunk.Update(localX, localY, localZ, function);
    }

    public void UpdateVoxel(Vector3I pos, VoxelOperator<T> function)
    {
        UpdateVoxel(pos.X, pos.Y, pos.Z, function);
    }

    public void UpdateVoxel(int x, int y, int z, Func<T, T> function)
    {
        int chunkX = x >> 4;
        int chunkY = y >> 4;
        int chunkZ = z >> 4;

        int localX = x & 15;
        int localY = y & 15;
        int localZ = z & 15;
        Vector3I chunkPos = new Vector3I(chunkX, chunkY, chunkZ);
        var chunk = GetOrCreateChunk(chunkPos);

        chunk.Update(localX, localY, localZ, function);
    }

    public void UpdateVoxel(Vector3I pos, Func<T, T> function)
    {
        UpdateVoxel(pos.X, pos.Y, pos.Z, function);
    }

    public VoxelChunk<T> GetOrCreateChunk(in Vector3I chunkPos) 
    {
        VoxelChunk<T>? chunk;
        if (!chunks.TryGetValue(chunkPos, out chunk))
        {
            chunk = new();
            chunks[chunkPos] = chunk;
        }
        return chunk;
    }

    /// <summary>
    /// Fill a section of the world with a value.
    /// </summary>
    /// <param name="pos1">Minimum world pos, inclusive.</param>
    /// <param name="pos2">Maximum world pos, inclusive.</param>
    /// <param name="value">The value.</param>
    public void Fill(Vector3I pos1, Vector3I pos2, T value)
    {
        Vector3I min = pos1.Min(pos2);
        Vector3I max = pos1.Max(pos2);

        Vector3I minChunk = GetChunk(min);
        Vector3I maxChunk = GetChunk(max);

        Vector3I localMin = GetPosInChunk(min);
        Vector3I localMax = GetPosInChunk(max);

        for (int chunkX = minChunk.X; chunkX <= maxChunk.X; chunkX++)
        {
            for (int chunkY = minChunk.Y; chunkY <= maxChunk.Y; chunkY++)
            {
                for (int chunkZ = minChunk.Z; chunkZ <= maxChunk.Z; chunkZ++)
                {
                    VoxelChunk<T> chunk = GetOrCreateChunk(new Vector3I(chunkX, chunkY, chunkZ));
                    // Shortcut if the entire chunk will be filled.
                    if (chunkX > minChunk.X && chunkX < maxChunk.X
                        && chunkY > minChunk.Y && chunkY < maxChunk.Y
                        && chunkZ > minChunk.Z && chunkZ < maxChunk.Z)
                    {
                        chunk.Fill(value);
                        continue;
                    }

                    int minX = chunkX > minChunk.X ? 0 : localMin.X;
                    int minY = chunkY > minChunk.Y ? 0 : localMin.Y;
                    int minZ = chunkZ > minChunk.Z ? 0 : localMin.Z;

                    int maxX = chunkX < maxChunk.X ? 15 : localMax.X;
                    int maxY = chunkY < maxChunk.Y ? 15 : localMax.Y;
                    int maxZ = chunkZ < maxChunk.Z ? 15 : localMax.Z;

                    for (int x = minX; x <= maxX; x++)
                    {
                        for (int y = minY; y <= maxY; y++)
                        {
                            for (int z = minZ; z <= maxZ; z++)
                            {
                                chunk.Set(x, y, z, value);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Return a copy of this world with a mapping function applied to each of the voxels.
    /// </summary>
    /// <typeparam name="V">Target voxel type.</typeparam>
    /// <param name="function">Mapping function.</param>
    /// <returns>The copy.</returns>
    public VoxelWorld<V> Transform<V>(Func<T, V> function)
    {
        VoxelWorld<V> result = new();
        foreach (var (pos, chunk) in chunks)
        {
            result.chunks.Add(pos, chunk.Transform(function));
        }
        return result;
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

    /// <summary>
    /// The minimum Position a voxel may have been placed, inclusive.
    /// </summary>
    public Vector3I GetMinPos()
    {
        if (!chunks.Any()) return Vector3I.Zero;

        Vector3I minChunk = Vector3I.MaxValue;
        foreach (var chunkPos in chunks.Keys)
        {
            if (chunkPos.X < minChunk.X)
                minChunk.X = chunkPos.X;
            if (chunkPos.Y < minChunk.Y)
                minChunk.Y = chunkPos.Y;
            if (chunkPos.Z < minChunk.Z)
                minChunk.Z = chunkPos.Z;
        }

        return minChunk * 16;
    }

    /// <summary>
    /// The maximum Position a voxel may have been placed, exclusive.
    /// </summary>
    public Vector3I GetMaxPos()
    {
        if (!chunks.Any()) return Vector3I.Zero;

        Vector3I maxChunk = Vector3I.MinValue;
        foreach (var chunkPos in chunks.Keys)
        {
            if (chunkPos.X > maxChunk.X)
                maxChunk.X = chunkPos.X;
            if (chunkPos.Y > maxChunk.Y)
                maxChunk.Y = chunkPos.Y;
            if (chunkPos.Z > maxChunk.Z)
                maxChunk.Z = chunkPos.Z;
        }

        return maxChunk * 16 + new Vector3I(16, 16, 16);
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
    /// Get the value at a specific location.
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
    /// Get the value at a specific location.
    /// </summary>
    /// <param name="pos">Local position.</param>
    /// <returns>The value.</returns>
    public T Get(Vector3I pos)
    {
        return Get(pos.X, pos.Y, pos.Z);
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

    public void Update(int x, int y, int z, VoxelOperator<T> function)
    {
        int index = x + (y * 16) + (z * 16 * 16);
        function.Invoke(ref data[index]);
    }

    public void Update(int x, int y, int z, Func<T, T> function)
    {
        int index = x + (y * 16) + (z * 16 * 16);
        data[index] = function.Invoke(data[index]);
    }

    public void Fill(T value)
    {
        Array.Fill(data, value);
    }

    public VoxelChunk<V> Transform<V>(Func<T, V> function)
    {
        VoxelChunk<V> result = new();
        for (int i = 0; i < data.Length; i++)
        {
            result.data[i] = function.Invoke(data[i]);
        }
        return result;
    }

    public VoxelChunk<T> Copy()
    {
        VoxelChunk<T> other = new();
        other.CopyFrom(this);
        return other;
    }

    public void CopyFrom(VoxelChunk<T> other)
    {
        Array.Copy(other.data, this.data, this.data.Length);
    }
}

internal static class Vector3IExtensions
{
    public static int SumComponents(this Vector3I vec)
    {
        return vec.X + vec.Y + vec.Z;
    }
}