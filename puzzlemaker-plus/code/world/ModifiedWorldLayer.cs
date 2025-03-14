﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// A floating modification layer over a world.
/// Allows commands to update a world's contents while keeping track of the original for undo/redo.
/// </summary>
public class ModifiedWorldLayer<T> : IVoxelView<T>
{
    public VoxelWorld<T> World { get; }

    private readonly Dictionary<Vector3I, VoxelChunk<T>> _updatedChunks = new();

    private readonly HashSet<Vector3I> _adjoiningChunks = new();

    public IDictionary<Vector3I, VoxelChunk<T>> UpdatedChunks => _updatedChunks;

    /// <summary>
    /// All chunks which have been updated or are next to a voxel that has been updated.
    /// Use this when meshing to avoid issues.
    /// </summary>
    public ISet<Vector3I> AdjoiningChunks => _adjoiningChunks;

    public ModifiedWorldLayer(VoxelWorld<T> world)
    {
        World = world;
    }

    public T? GetVoxel(int x, int y, int z)
    {
        return GetVoxel(new Vector3I(x, y, z));
    }

    public T? GetVoxel(Vector3I pos)
    {
        VoxelChunk<T>? chunk = GetReadableChunk(pos.GetChunk());
        if (chunk == null)
            return default;

        return chunk.GetVoxel(pos.GetChunkLocalPos());
    }

    public T? SetVoxel(int x, int y, int z, T value)
    {
        return SetVoxel(new Vector3I(x, y, z), value);
    }

    public T? SetVoxel(Vector3I pos, T value)
    {
        VoxelChunk<T> chunk = GetWritableChunk(pos.GetChunk());
        MarkVoxelUpdated(pos);
        return chunk.SetVoxel(pos.GetChunkLocalPos(), value);
    }

    public void UpdateVoxel(int x, int y, int z, Func<T, T> function)
    {
        Vector3I pos = new Vector3I(x, y, z);
        Vector3I localPos = pos.GetChunkLocalPos();

        VoxelChunk<T> chunk = GetWritableChunk(pos.GetChunk());
        MarkVoxelUpdated(pos);

        chunk.UpdateVoxel(localPos.X, localPos.Y, localPos.Z, function);
    }

    public void Fill(Vector3I pos1, Vector3I pos2, T value)
    {
        Vector3I min = pos1.Min(pos2);
        Vector3I max = pos1.Max(pos2);
        // TODO: make the optimized implementation work.
        for (int x = min.X; x <= max.X; x++)
        {
            for (int y = min.Y; y <= max.Y; y++)
            {
                for (int z = min.Z; z <= max.Z; z++)
                {
                    SetVoxel(x, y, z, value);
                }
            }
        }
        // UpdateBox(pos1, pos2, (pos, val) => val);
    }

    public void UpdateBox(Vector3I pos1, Vector3I pos2, Func<Vector3I, T, T> function, bool _readOnly = false)
    {
        Vector3I min = pos1.Min(pos2);
        Vector3I max = pos1.Max(pos2);

        // TODO: make the optimized implementation work.
        for (int x = min.X; x <= max.X; x++)
        {
            for (int y = min.Y; y <= max.Y; y++)
            {
                for (int z = min.Z; z <= max.Z; z++)
                {
                    UpdateVoxel(x, y, z, val => function(new Vector3I(x, y, z), val));
                }
            }
        }

        // Vector3I minChunk = VoxelWorld<T>.GetChunk(min);
        // Vector3I maxChunk = VoxelWorld<T>.GetChunk(max);

        // Vector3I localMin = VoxelWorld<T>.GetPosInChunk(min);
        // Vector3I localMax = VoxelWorld<T>.GetPosInChunk(max);

        // for (int chunkX = minChunk.X; chunkX <= maxChunk.X; chunkX++)
        // {
        //     for (int chunkY = minChunk.Y; chunkY <= maxChunk.Y; chunkY++)
        //     {
        //         for (int chunkZ = minChunk.Z; chunkZ <= maxChunk.Z; chunkZ++)
        //         {
        //             VoxelChunk<T> chunk = GetWritableChunk(new Vector3I(chunkX, chunkY, chunkZ));

        //             int minX = chunkX > minChunk.X ? 0 : localMin.X;
        //             int minY = chunkY > minChunk.Y ? 0 : localMin.Y;
        //             int minZ = chunkZ > minChunk.Z ? 0 : localMin.Z;

        //             int maxX = chunkX < maxChunk.X ? 15 : localMax.X;
        //             int maxY = chunkY < maxChunk.Y ? 15 : localMax.Y;
        //             int maxZ = chunkZ < maxChunk.Z ? 15 : localMax.Z;

        //             for (int x = minX; x <= maxX; x++)
        //             {
        //                 for (int y = minY; y <= maxY; y++)
        //                 {
        //                     for (int z = minZ; z <= maxZ; z++)
        //                     {
        //                         chunk.Update(x, y, z, val => function(new Vector3I(x, y, z), val));
        //                     }
        //                 }
        //             }
        //         }
        //     }
        // }
    }
    

    private VoxelChunk<T>? GetReadableChunk(Vector3I chunkPos)
    {
        if (_updatedChunks.TryGetValue(chunkPos, out var chunk))
        {
            return chunk;
        }
        else if (World.Chunks.TryGetValue(chunkPos, out chunk))
        {
            return chunk;
        }
        else return null;
    }

    private VoxelChunk<T> GetWritableChunk(Vector3I chunkPos)
    {
        VoxelChunk<T>? chunk;
        if (_updatedChunks.TryGetValue(chunkPos, out chunk))
        {
            return chunk;
        }
        chunk = new();
        if (World.Chunks.TryGetValue(chunkPos, out var baseChunk))
        {
            baseChunk.CopyTo(chunk);
        }
        _updatedChunks[chunkPos] = chunk;
        return chunk;
    }

    private void MarkVoxelUpdated(in Vector3I worldPos)
    {
        Vector3I chunkPos = worldPos.GetChunk();
        AdjoiningChunks.Add(chunkPos);

        Vector3I localPos = worldPos.GetChunkLocalPos();

        // Mark adjoining chunks updated as well for meshing
        if (localPos.X == 0)
            AdjoiningChunks.Add(chunkPos + new Vector3I(-1, 0, 0));
        else if (localPos.X == 15)
            AdjoiningChunks.Add(chunkPos + new Vector3I(1, 0, 0));
        if (localPos.Y == 0)
            AdjoiningChunks.Add(chunkPos + new Vector3I(0, -1, 0));
        else if (localPos.Y == 15)
            AdjoiningChunks.Add(chunkPos + new Vector3I(0, 1, 0));
        if (localPos.Z == 0)
            AdjoiningChunks.Add(chunkPos + new Vector3I(0, 0, -1));
        else if (localPos.Z == 15)
            AdjoiningChunks.Add(chunkPos + new Vector3I(0, 0, 1));
    }
}
