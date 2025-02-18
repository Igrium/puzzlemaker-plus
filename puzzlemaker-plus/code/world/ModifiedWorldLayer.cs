using System;
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

    public IDictionary<Vector3I, VoxelChunk<T>> UpdatedChunks => _updatedChunks;
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
        VoxelChunk<T>? chunk = GetReadableChunk(VoxelWorld<T>.GetChunk(pos));
        if (chunk == null)
            return default;

        return chunk.Get(VoxelWorld<T>.GetPosInChunk(pos));
    }

    public T? SetVoxel(int x, int y, int z, T value)
    {
        return SetVoxel(new Vector3I(x, y, z), value);
    }

    public T? SetVoxel(Vector3I pos, T value)
    {
        VoxelChunk<T> chunk = GetWritableChunk(VoxelWorld<T>.GetChunk(pos));
        return chunk.Set(VoxelWorld<T>.GetPosInChunk(pos), value);
    }

    public void UpdateVoxel(int x, int y, int z, Func<T, T> function)
    {
        Vector3I pos = new Vector3I(x, y, z);
        Vector3I localPos = VoxelWorld<T>.GetPosInChunk(pos);

        VoxelChunk<T> chunk = GetWritableChunk(VoxelWorld<T>.GetChunk(pos));

        chunk.Update(localPos.X, localPos.Y, localPos.Z, function);
    }

    public void Fill(Vector3I pos1, Vector3I pos2, T value)
    {
        UpdateBox(pos1, pos2, (pos, val) => val);
    }

    public void UpdateBox(Vector3I pos1, Vector3I pos2, Func<Vector3I, T, T> function, bool _readOnly = false)
    {
        Vector3I min = pos1.Min(pos2);
        Vector3I max = pos1.Max(pos2);

        Vector3I minChunk = VoxelWorld<T>.GetChunk(min);
        Vector3I maxChunk = VoxelWorld<T>.GetChunk(max);

        Vector3I localMin = VoxelWorld<T>.GetPosInChunk(min);
        Vector3I localMax = VoxelWorld<T>.GetPosInChunk(max);

        for (int chunkX = minChunk.X; chunkX <= maxChunk.X; chunkX++)
        {
            for (int chunkY = minChunk.Y; chunkY <= maxChunk.Y; chunkY++)
            {
                for (int chunkZ = minChunk.Z; chunkZ <= maxChunk.Z; chunkZ++)
                {
                    VoxelChunk<T> chunk = GetWritableChunk(new Vector3I(chunkX, chunkY, chunkZ));

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
                                chunk.Update(x, y, z, val => function(new Vector3I(x, y, z), val));
                            }
                        }
                    }
                }
            }
        }
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
}
