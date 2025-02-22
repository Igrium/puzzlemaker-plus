using System;
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// Internally, voxels are represented in an "inverted" fashon. That is portalability flags represent the
/// portalability of the surface facing into the voxel. Sometimes, however, it's desirable to represent
/// it the other way around. This class lets that happen.
/// </summary>
public static class InverseVoxels 
{
    // TODO: Needs to be updated with subdivision level once implemented.
    public static PuzzlemakerVoxel GetInverseVoxel(this IVoxelView<PuzzlemakerVoxel> world, Vector3I pos) 
    {
        PuzzlemakerVoxel voxel = world.GetVoxel(pos);
        foreach (var dir in Enum.GetValues<Direction>()) 
        {
            var other = world.GetVoxel(pos + dir.GetNormal());
            voxel.SetPortalability(dir, other.IsPortalable(dir.Opposite()));
        }
        return voxel;
    }

    public static void SetInverseVoxel(this IVoxelView<PuzzlemakerVoxel> world, Vector3I pos, PuzzlemakerVoxel voxel) 
    {
        foreach (var dir in Enum.GetValues<Direction>()) 
        {
            world.UpdateVoxel(pos + dir.GetNormal(), vox => vox.WithPortalability(dir.Opposite(), voxel.IsPortalable(dir)));
        }
        world.UpdateVoxel(pos, vox => vox.WithOpen(voxel.IsOpen));
    }

    /// <summary>
    /// Set a box of inverse voxels. This implementation is more efficient than SetInverseVoxel in a loop because it avoids
    /// redundant loops on the interior of the box.
    /// </summary>
    /// <param name="world">Voxel world to use.</param>
    /// <param name="pos1">One corner of the box, inclusive.</param>
    /// <param name="pos2">The other corner of the box, inclusive.</param>
    /// <param name="voxel">Inverse voxel to set.</param>
    public static void SetInverseVoxelBox(this IVoxelView<PuzzlemakerVoxel> world, Vector3I pos1, Vector3I pos2, PuzzlemakerVoxel voxel)
    {
        Vector3I min = pos1.Min(pos2);
        Vector3I max = pos1.Max(pos2);

        // All the interior voxels will look the same
        PuzzlemakerVoxel reversed = voxel.GetReversed();

        // Update a voxel outside the box to have the right portalability inwards.
        void updateDirection(Vector3I pos, Direction direction) 
        {
            world.UpdateVoxel(pos + direction.GetNormal(), vox => vox.WithPortalability(direction.Opposite(), voxel.IsPortalable(direction)));
        }
        
        for (int x = min.X; x <= max.X; x++) 
        {
            for (int y = min.Y; y <= max.Y; y++)
            {
                for (int z = min.Z; z <= max.Z; z++) 
                {
                    Vector3I pos = new Vector3I(x, y, z);
                    world.SetVoxel(pos, reversed);
                    if (x == min.X)
                        updateDirection(pos, Direction.Left);
                    if (x == max.X)
                        updateDirection(pos, Direction.Right);
                    if (y == min.Y)
                        updateDirection(pos, Direction.Down);
                    if (y == max.Y)
                        updateDirection(pos, Direction.Up);
                    if (z == min.Z)
                        updateDirection(pos, Direction.Forward);
                    if (z == max.Z)
                        updateDirection(pos, Direction.Back);
                }
            }
        }
    }

    private static PuzzlemakerVoxel GetReversed(this PuzzlemakerVoxel voxel)
    {
        PuzzlemakerVoxel result = voxel;
        foreach (var dir in Enum.GetValues<Direction>())
        {
            result.SetPortalability(dir.Opposite(), voxel.IsPortalable(dir));
        }
        return result;
    }
}