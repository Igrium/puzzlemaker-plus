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
/// While this class has a lot of helper functions, the struct itself is only one byte.
/// </summary>
public struct PuzzlemakerVoxel
{
    [Flags]
    public enum VoxelFlags
    {
        Open = 0,
        XP = 1,
        XN = 2,
        YP = 4,
        YN = 8,
        ZP = 16,
        ZN = 32
    }

    public byte Flags;

    public bool IsOpen => HasFlag(VoxelFlags.Open);
    public bool XPositive => HasFlag(VoxelFlags.XP);
    public bool XNegative => HasFlag(VoxelFlags.XN);
    public bool YPositive => HasFlag(VoxelFlags.YP);
    public bool YNegative => HasFlag(VoxelFlags.YN);
    public bool ZPositive => HasFlag(VoxelFlags.ZP);
    public bool ZNegative => HasFlag(VoxelFlags.ZN);

    public PuzzlemakerVoxel SetXPositive(bool portalable)
    {
        return portalable ? SetFlag(VoxelFlags.XP) : ClearFlag(VoxelFlags.XP);
    }

    public PuzzlemakerVoxel SetXNegative(bool portalable)
    {
        return portalable ? SetFlag(VoxelFlags.XN) : ClearFlag(VoxelFlags.XN);
    }

    public PuzzlemakerVoxel SetYPositive(bool portalable)
    {
        return portalable ? SetFlag(VoxelFlags.YP) : ClearFlag(VoxelFlags.YP);
    }

    public PuzzlemakerVoxel SetYNegative(bool portalable)
    {
        return portalable ? SetFlag(VoxelFlags.YN) : ClearFlag(VoxelFlags.YN);
    }

    public PuzzlemakerVoxel SetZPositive(bool portalable)
    {
        return portalable ? SetFlag(VoxelFlags.ZP) : ClearFlag(VoxelFlags.ZP);
    }

    public PuzzlemakerVoxel SetZNegative(bool portalable)
    {
        return portalable ? SetFlag(VoxelFlags.ZN) : ClearFlag(VoxelFlags.ZN);
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
}
