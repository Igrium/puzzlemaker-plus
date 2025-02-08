using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus;

public class PuzzlemakerWorld : VoxelWorld<PuzzlemakerVoxel>
{
    
}

/// <summary>
/// A single voxel in the puzzlemaker voxel world.
/// While this class has a lot of helper functions, the struct itself is only two bytes.
/// </summary>
public struct PuzzlemakerVoxel
{
    [Flags]
    public enum VoxelFlags
    {
        Open = 0,
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8,
        Front = 16,
        Back = 32
    }

    /// <summary>
    /// The bit flags of this voxel. Contains data about whether it's open and its portalability.
    /// </summary>
    public byte Flags;

    /// <summary>
    /// The subdivision level of the voxel. 0 = 128 hammer units; 1 = 64 hammer units; 2 = 32 hammer units.
    /// </summary>
    public byte Subdivision;

    public bool IsOpen => HasFlag(VoxelFlags.Open);
    public bool UpPortalable => HasFlag(VoxelFlags.Up);
    public bool DownPortalable => HasFlag(VoxelFlags.Down);
    public bool LeftPortalable => HasFlag(VoxelFlags.Left);
    public bool RightPortalable => HasFlag(VoxelFlags.Right);
    public bool FrontPortalable => HasFlag(VoxelFlags.Front);
    public bool BackPortalable => HasFlag(VoxelFlags.Back);

    public PuzzlemakerVoxel WithOpen(bool open)
    {
        return open ? this.SetFlag(VoxelFlags.Open) : this.ClearFlag(VoxelFlags.Open);
    }

    public bool HasFlag(VoxelFlags flag)
    {
        return ((Flags & (byte)flag) == (byte)flag);
    }

    public PuzzlemakerVoxel SetFlag(VoxelFlags flag)
    {
        PuzzlemakerVoxel result = this;
        result.Flags |= (byte)flag;
        return result;
    }

    public PuzzlemakerVoxel SetFlags(params VoxelFlags[] flags)
    {
        PuzzlemakerVoxel result = this;
        foreach (VoxelFlags flag in flags)
        {
            result.Flags |= (byte)flag;
        }
        return result;
    }

    public PuzzlemakerVoxel ClearFlag(VoxelFlags flag)
    {
        PuzzlemakerVoxel result = this;
        Flags &= (byte)~flag;
        return result;
    }

    public PuzzlemakerVoxel ClearFlags(params VoxelFlags[] flags)
    {
        PuzzlemakerVoxel result = this;
        foreach (VoxelFlags flag in flags)
        {
            result.Flags &= (byte)~flag;
        }
        return result;
    }

    public PuzzlemakerVoxel SetSubdivision(byte subdivision)
    {
        PuzzlemakerVoxel result = this;
        result.Subdivision = subdivision;
        return result;
    }
}
