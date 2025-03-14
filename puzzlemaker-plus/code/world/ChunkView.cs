using System;
using Godot;

namespace PuzzlemakerPlus;

public class ChunkView<T> : IVoxelView<T>
{
    private readonly VoxelWorld<T> _world;
    private readonly Vector3I _chunkOffset;
    private readonly VoxelChunk<T> _chunk;

    public ChunkView(VoxelWorld<T> world, Vector3I chunkPos, VoxelChunk<T> chunk)
    {
        _world = world;
        _chunkOffset = chunkPos.ChunkStartPos();
        _chunk = chunk;
    }

    public ChunkView(VoxelWorld<T> world, Vector3I chunkPos)
    {
        _world = world;
        _chunkOffset = chunkPos.ChunkStartPos();
        _chunk = world.GetOrCreateChunk(chunkPos);
    }

    public void Fill(Vector3I pos1, Vector3I pos2, T value)
    {
        if (IsInRange(pos1) && IsInRange(pos2))
        {
            _chunk.Fill(pos1, pos2, value);
        }
        else
        {
            _world.Fill(pos1 + _chunkOffset, pos2 + _chunkOffset, value);
        }
    }

    public T? GetVoxel(int x, int y, int z)
    {
        Vector3I pos = new Vector3I(x, y, z);
        if (IsInRange(in pos))
        {
            return _chunk.GetVoxel(pos);
        }
        else
        {
            return _world.GetVoxel(pos + _chunkOffset);
        }
    }

    public T? SetVoxel(int x, int y, int z, T value)
    {
        Vector3I pos = new Vector3I(x, y, z);
        if (IsInRange(in pos))
        {
            return _chunk.SetVoxel(pos, value);
        }
        else
        {
            return _world.SetVoxel(pos + _chunkOffset, value);
        }
    }

    public void UpdateBox(Vector3I pos1, Vector3I pos2, Func<Vector3I, T, T> function, bool readOnly = false)
    {
        if (IsInRange(in pos1) && IsInRange(in pos2))
        {
            _chunk.UpdateBox(pos1, pos2, function, readOnly);
        }
        else
        {
            _world.UpdateBox(pos1 + _chunkOffset, pos2 + _chunkOffset, (pos, val) => function(pos - _chunkOffset, val), readOnly);
        }
    }

    public void UpdateVoxel(int x, int y, int z, Func<T, T> function)
    {
        Vector3I pos = new Vector3I(x, y, z);
        if (IsInRange(in pos))
        {
            _chunk.UpdateVoxel(x, y, z, function);
        }
        else
        {
            _world.UpdateVoxel(pos + _chunkOffset, function);
        }
    }

    private static bool IsInRange(in Vector3I pos)
    {
        return 0 <= pos.X && pos.X < 16
            && 0 <= pos.Y && pos.Y < 16
            && 0 <= pos.Z && pos.Z < 16;
    }
}
