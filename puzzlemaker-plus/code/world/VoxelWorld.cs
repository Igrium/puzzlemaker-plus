using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;

namespace PuzzlemakerPlus;

/// <summary>
/// Called to update the value in a voxel without a bunch of indirection.
/// This doesn't work right now and I can't figure out how to make it work.
/// </summary>
/// <typeparam name="T">Voxel type.</typeparam>
/// <param name="value">A reference to the voxel's value. Update this to update the voxel.</param>
public delegate void VoxelOperator<T>(ref T value);

public partial class VoxelWorld<T> : RefCounted, IVoxelView<T>
{
    public const int CHUNK_SIZE = 16;

    private readonly ConcurrentDictionary<Vector3I, VoxelChunk<T>> _chunks = new();

    /// <summary>
    /// A dictionary of all the _chunks in this world.
    /// </summary>
    public IDictionary<Vector3I, VoxelChunk<T>> Chunks => _chunks;

    public T? GetVoxel(int x, int y, int z)
    {
        Vector3I pos = new Vector3I(x, y, z);
        Vector3I chunkPos = pos.GetChunk();
        Vector3I localPos = pos.GetChunkLocalPos();

        if (_chunks.TryGetValue(chunkPos, out var chunk))
        {
            return chunk.GetVoxel(localPos);
        }
        else return default;
        
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
        return chunk.SetVoxel(localX, localY, localZ, value);
    }

    public T? SetVoxel(Vector3I pos, T value)
    {
        return SetVoxel(pos.X, pos.Y, pos.Z, value);
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

        chunk.UpdateVoxel(localX, localY, localZ, function);
    }

    public void UpdateVoxel(Vector3I pos, Func<T, T> function)
    {
        UpdateVoxel(pos.X, pos.Y, pos.Z, function);
    }

    public VoxelChunk<T> GetOrCreateChunk(in Vector3I chunkPos) 
    {
        return _chunks.GetOrAdd(chunkPos, pos => new VoxelChunk<T>());
    }

    /// <summary>
    /// Fill a section of the world with a value.
    /// </summary>
    /// <param name="pos1">Minimum world pos, inclusive.</param>
    /// <param name="pos2">Maximum world pos, inclusive.</param>
    /// <param name="value">The value.</param>
    public void Fill(Vector3I pos1, Vector3I pos2, T value)
    {
        UpdateBox(pos1, pos2, (pos, val) => value);
    }

    public void UpdateBox(Vector3I pos1, Vector3I pos2, Func<Vector3I, T, T> function, bool _readOnly = false)
    {
        Vector3I min = pos1.Min(pos2);
        Vector3I max = pos1.Max(pos2);

        Vector3I minChunk = min.GetChunk();
        Vector3I maxChunk = max.GetChunk();

        Vector3I localMin = min.GetChunkLocalPos();
        Vector3I localMax = max.GetChunkLocalPos();

        for (int chunkX = minChunk.X; chunkX <= maxChunk.X; chunkX++)
        {
            for (int chunkY = minChunk.Y; chunkY <= maxChunk.Y; chunkY++)
            {
                for (int chunkZ = minChunk.Z; chunkZ <= maxChunk.Z; chunkZ++)
                {
                    VoxelChunk<T> chunk = GetOrCreateChunk(new Vector3I(chunkX, chunkY, chunkZ));

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
                                chunk.UpdateVoxel(x, y, z, val => function(new Vector3I(x, y, z), val));
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
        lock (_chunks)
        {
            foreach (var (pos, chunk) in _chunks)
            {
                result._chunks[pos] = chunk.Transform(function);
            }
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
                        T val = chunk.GetVoxel(x, y, z);
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
        if (!_chunks.Any()) return Vector3I.Zero;

        Vector3I minChunk = Vector3I.MaxValue;
        foreach (var chunkPos in _chunks.Keys)
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
        if (!_chunks.Any()) return Vector3I.Zero;

        Vector3I maxChunk = Vector3I.MinValue;
        foreach (var chunkPos in _chunks.Keys)
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
public class VoxelChunk<T> : IVoxelView<T>
{
    [JsonIgnore]
    private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
    private readonly T[] _data = new T[16 * 16 * 16];

    /// <summary>
    /// The flat array containing the chunk's _data.
    /// </summary>
    public T[] Data { get => _data; }

    /// <summary>
    /// GetVoxel the value at a specific location.
    /// </summary>
    /// <param name="x">Local X coordinate.</param>
    /// <param name="y">Local Y coordinate.</param>
    /// <param name="z">Local Z coordinate.</param>
    /// <returns>The value.</returns>
    public T GetVoxel(int x, int y, int z)
    {
        AssertValidIndex(x, y, z);
        int index = x + (y * 16) + (z * 16 * 16);
        return _data[index];
    }
    
    /// <summary>
    /// GetVoxel the value at a specific location.
    /// </summary>
    /// <param name="pos">Local position.</param>
    /// <returns>The value.</returns>
    public T GetVoxel(Vector3I pos)
    {
        return GetVoxel(pos.X, pos.Y, pos.Z);
    }

    /// <summary>
    /// Set the value at a specific location.
    /// </summary>
    /// <param name="x">Local X coordinate.</param>
    /// <param name="y">Local Y coordinate.</param>
    /// <param name="z">Local Z coordinate.</param>
    /// <param name="value">New value.</param>
    /// <returns>The previous value.</returns>
    public T SetVoxel(int x, int y, int z, T value)
    {
        AssertValidIndex(x, y, z);
        int index = x + (y * 16) + (z * 16 * 16);
        T prev = _data[index];
        _data[index] = value;
        return prev;
    }

    /// <summary>
    /// Set the value at a specific location.
    /// </summary>
    /// <param name="pos">Local position.</param>
    /// <param name="value">New value.</param>
    /// <returns>The previous value.</returns>
    public T SetVoxel(Vector3I pos, T value)
    {
        return SetVoxel(pos.X, pos.Y, pos.Z, value);
    }

    public void UpdateVoxel(int x, int y, int z, Func<T, T> function)
    {
        AssertValidIndex(x, y, z);

        int index = x + (y * 16) + (z * 16 * 16);
        _data[index] = function(_data[index]);
    }

    private void AssertValidIndex(int x, int y, int z)
    {
        if (x < 0 || x >= 16)
            throw new ArgumentOutOfRangeException(nameof(x), x, "X param out of range");
        if (y < 0 || y >= 16)
            throw new ArgumentOutOfRangeException(nameof(x), y, "Y param out of range");
        else if (z < 0 || y >= 16)
            throw new ArgumentOutOfRangeException(nameof(z), z, "Z param out of range");
    }

    private void AssertValidIndex(in Vector3I vec)
    {
        AssertValidIndex(vec.X, vec.Y, vec.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetIndex(int x, int y, int z)
    {
        return x + (y * 16) + (z * 16 * 16);
    }

    public void Fill(T value)
    {
        Array.Fill(_data, value);
    }


    public VoxelChunk<V> Transform<V>(Func<T, V> function)
    {
        VoxelChunk<V> result = new();
        for (int i = 0; i < _data.Length; i++)
        {
            result._data[i] = function.Invoke(_data[i]);
        }
        return result;
    }

    public void CopyTo(VoxelChunk<T> other)
    {
        Array.Copy(_data, other._data, _data.Length);
    }

    public void Fill(Vector3I pos1, Vector3I pos2, T value)
    {
        AssertValidIndex(pos1);
        AssertValidIndex(pos2);

        for (int x = pos1.X; x <= pos2.X; x++)
        {
            for (int y = pos1.Y; y <= pos2.Y; y++)
            {
                for (int z = pos1.Z; z <= pos2.Z; z++)
                {
                    _data[GetIndex(x, y, z)] = value;
                }
            }
        }
    }

    public void UpdateBox(Vector3I pos1, Vector3I pos2, Func<Vector3I, T, T> function, bool readOnly = false)
    {
        AssertValidIndex(pos1);
        AssertValidIndex(pos2);

        for (int x = pos1.X; x <= pos2.X; x++)
        {
            for (int y = pos1.Y; y <= pos2.Y; y++)
            {
                for (int z = pos1.Z; z <= pos2.Z; z++)
                {
                    int index = GetIndex(x, y, z);
                    _data[index] = function(new Vector3I(x, y, z), _data[index]);
                }
            }
        }
    }
}

internal static class Vector3IExtensions
{
    public static int SumComponents(this Vector3I vec)
    {
        return vec.X + vec.Y + vec.Z;
    }
}