using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// A collection of basic operators that involve selections.
/// </summary>
public static class SelectionUtils
{
    /// <summary>
    /// Set the portalability of all faces in a selection.
    /// </summary>
    /// <param name="selection">Selection box.</param>
    /// <param name="world">World to apply to.</param>
    public static void SetPortalable(in Aabb selection, VoxelWorld<PuzzlemakerVoxel> world, bool portalable)
    {
        foreach (var (pos, face) in EnumerateFaces(in selection))
        {
            world.UpdateVoxel(pos, block => block.WithPortalability(face, portalable));
        }
    }

    /// <summary>
    /// Compare the amount of portalable vs non-portalable faces in a selection.
    /// </summary>
    /// <param name="selection">Selection box.</param>
    /// <param name="world">World to check.</param>
    /// <returns>A positive integer if there's more portalable faces, 0 if there even, or a negative integer if there's more non-portalable faces.</returns>
    public static int AveragePortalability(in Aabb selection, VoxelWorld<PuzzlemakerVoxel> world)
    {
        int nonPortalable = 0;
        int portalable = 0;

        foreach(var (pos, face) in EnumerateFaces(in selection))
        {
            PuzzlemakerVoxel voxel = world.GetVoxel(pos);
            if (voxel.IsOpen)
            {
                if (voxel.IsPortalable(face))
                    portalable++;
                else
                    nonPortalable++;
            }
        }

        return portalable - nonPortalable;
    }

    /// <summary>
    /// Enumerate over all the faces in a selection.
    /// </summary>
    /// <param name="selection">Selection box.</param>
    /// <param name="exact">If true, each face must be entirely in the exact selection, othewise, selection is rounded to nearest int.</param>
    /// <returns>All faces in the selection.</returns>
    public static IEnumerable<(Vector3I, Direction)> EnumerateFaces(in Aabb selection, bool exact = false)
    {
        Vector3I min = exact ? selection.Position.CeilInt() : selection.Position.RoundInt();
        Vector3I max = exact ? selection.End.FloorInt() : selection.End.RoundInt();
        return EnumerateFaces(min, max);
    }

    /// <summary>
    /// Enumerate over all the faces in a selection.
    /// </summary>
    /// <param name="min">Selection minimum pos, inclusive.</param>
    /// <param name="max">Selection maximum pos, exclusive.</param>
    /// <returns>All faces in the selection.</returns>
    public static IEnumerable<(Vector3I, Direction)> EnumerateFaces(Vector3I min, Vector3I max)
    {
        if (min == max)
            yield break;

        // If this is a 2D selection, find the axis.
        int axis;
        if (min.X == max.X)
            axis = 0;
        else if (min.Y == max.Y)
            axis = 1;
        else if (min.Z == max.Z)
            axis = 2;
        else
        {
            foreach (var pos in EnumerateBox(min, max))
            {
                yield return (pos, Direction.Up);
                yield return (pos, Direction.Down);
                yield return (pos, Direction.Left);
                yield return (pos, Direction.Right);
                yield return (pos, Direction.Forward);
                yield return (pos, Direction.Back);
            }
            yield break;
        }

        Vector3I normal = new Vector3I();
        normal[axis] = 1;
        Direction positive = Directions.FromAxis(axis, false);
        Direction negative = Directions.FromAxis(axis, true);

        // Add 1 to 2d axis because this is exclusive.
        foreach (var pos in EnumerateBox(min, max + normal))
        {
            yield return (pos, negative);
            yield return (pos - normal, positive);
        }
    }

    private static IEnumerable<Vector3I> EnumerateBox(Vector3I min, Vector3I max, bool inclusive = false)
    {
        if (inclusive)
            max += new Vector3I(1, 1, 1);

        for (int x = min.X; x < max.X; x++)
        {
            for (int y = min.Y; y < max.Y; y++)
            {
                for (int z = min.Z; z < max.Z; z++)
                {
                    yield return new Vector3I(x, y, z);
                }
            }
        }
    }

}
