using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus;

public interface IVoxelView<T>
{
    public T? GetVoxel(int x, int y, int z);

    public T? GetVoxel(Vector3I pos)
    {
        return GetVoxel(pos.X, pos.Y, pos.Z);
    }

    public T? SetVoxel(int x, int y, int z, T value);

    public T? SetVoxel(Vector3I pos, T value)
    {
        return SetVoxel(pos.X, pos.Y, pos.Z, value);
    }

    public void UpdateVoxel(int x, int y, int z, Func<T, T> function);

    public void UpdateVoxel(Vector3I pos, Func<T, T> function)
    {
        UpdateVoxel(pos.X, pos.Y, pos.Z, function);
    }

    /// <summary>
    /// Fill a section of the world with a value.
    /// </summary>
    /// <param name="pos1">Minimum world pos, inclusive.</param>
    /// <param name="pos2">Maximum world pos, inclusive.</param>
    /// <param name="value">The value.</param>
    public void Fill(Vector3I pos1, Vector3I pos2, T value);

    /// <summary>
    /// Update each block within a box using a given function. More efficient than calling SetVoxel in a loop because it can cache chunk references.
    /// </summary>
    /// <param name="pos1">Minimum world pos, inclusive.</param>
    /// <param name="pos2">Maximum world pos, inclusive</param>
    /// <param name="function">Update function.</param>
    public void UpdateBox(Vector3I pos1, Vector3I pos2, Func<Vector3I, T, T> function);
}
