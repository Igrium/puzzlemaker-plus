﻿using System;
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
    /// Compute the voxel locations that could be updated for a given selection.
    /// </summary>
    /// <param name="selection">The selection. If the selection is 3D, voxels must be entirely encased to be selected. 
    ///                         If it's 2D, they must share a face with the selection plane.</param>
    /// <returns>The minimum and maximum selected positions, both inclusive.</returns>
    public static (Vector3I, Vector3I) GetSelectedVoxels(in Aabb selection)
    {
        Vector3I min = selection.Position.RoundInt();
        Vector3I max = selection.End.RoundInt();

        if (min == max)
        {
            return (min, min);
        }

        int axis;
        if (min.X == max.X)
            axis = 0;
        else if (min.Y == max.Y)
            axis = 1;
        else if (min.Z == max.Z)
            axis = 2;
        else
            return (min, max - new Vector3I(1, 1, 1)); // Convert exclusive to inclusive.

        //GD.Print(axis);
        // Convert exclusive to inclusive
        Vector3I normal = new();
        normal[axis] = 2;

        min -= normal; // Expand min 1 block back in direction of 2D plane.

        return (min, max);
    }

    /// <summary>
    /// Set the portalability of all faces in a selection.
    /// </summary>
    /// <param name="selection">Selection box.</param>
    /// <param name="world">World to apply to.</param>
    public static void SetPortalable(Aabb selection, IVoxelView<PuzzlemakerVoxel> world, bool portalable)
    {
        foreach (var (pos, face) in EnumerateFaces(in selection, true))
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
    public static int AveragePortalability(in Aabb selection, IVoxelView<PuzzlemakerVoxel> world)
    {
        int nonPortalable = 0;
        int portalable = 0;

        void markPortalability(bool isPortalable)
        {
            if (isPortalable)
                portalable++;
            else
                nonPortalable++;
        }

        foreach(var (pos, face) in EnumerateFaces(in selection, true))
        {
            PuzzlemakerVoxel voxel = world.GetVoxel(pos);
            if (voxel.IsOpen)
            {
                if (face.HasValue)
                {
                    markPortalability(voxel.IsPortalable(face.Value));
                }
                else
                {
                    foreach (Direction dir in Enum.GetValues<Direction>())
                    {
                        markPortalability(voxel.IsPortalable(dir));
                    }
                }
            }
        }

        return portalable - nonPortalable;
    }

    /// <summary>
    /// Enumerate over all the faces in a selection.
    /// </summary>
    /// <param name="selection">Selection box.</param>
    /// <param name="includeInwardFaces">If set, faces belonging to voxels outside the box will also be returned if they overlap the box. Always true for 2D selections.</param>
    /// <returns>All faces in the selection.</returns>
    public static IEnumerable<(Vector3I, Direction?)> EnumerateFaces(in Aabb selection, bool includeInwardFaces = false)
    {
        Vector3I min =  selection.Position.RoundInt();
        Vector3I max = selection.End.RoundInt();
        return EnumerateFaces(min, max, includeInwardFaces);
    }

    /// <summary>
    /// Enumerate over all the faces in a selection.
    /// </summary>
    /// <param name="min">Selection minimum pos, inclusive.</param>
    /// <param name="max">Selection maximum pos, exclusive.</param>
    /// <param name="includeInwardFaces">If set, faces belonging to voxels outside the box will also be returned if they overlap the box. Always true for 2D selections.</param>
    /// <returns>All faces in the selection. If direction is null, the entire block is selected.</returns>
    public static IEnumerable<(Vector3I, Direction?)> EnumerateFaces(Vector3I min, Vector3I max, bool includeInwardFaces = false)
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
                yield return (pos, null);

                // Check if we're at the edge and include inward faces.
                if (includeInwardFaces)
                {
                    if (pos.X == min.X)
                        yield return (pos + new Vector3I(-1, 0, 0), Direction.Left);
                    if (pos.X == max.X - 1)
                        yield return (pos + new Vector3I(1, 0, 0), Direction.Right);
                    if (pos.Y == min.Y)
                        yield return (pos + new Vector3I(0, -1, 0), Direction.Down);
                    if (pos.Y == max.Y - 1)
                        yield return (pos + new Vector3I(0, 1, 0), Direction.Up);
                    if (pos.Z == min.Z)
                        yield return (pos + new Vector3I(0, 0, -1), Direction.Forward);
                    if (pos.Z == max.Z - 1)
                        yield return (pos + new Vector3I(0, 0, 1), Direction.Back);
                }
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

    //public static bool IsFaceSelected(in Aabb selection, Vector3I pos, Direction face, bool exact = false)
    //{
    //    Vector3I min = exact ? selection.Position.CeilInt() : selection.Position.RoundInt();
    //    Vector3I max = exact ? selection.End.FloorInt() : selection.End.RoundInt();
    //    return IsFaceSelected(min, max, pos, face);
    //}

    //public static bool IsFaceSelected(Vector3I min, Vector3I max, Vector3I pos, Direction face)
    //{
    //    if (min == max)
    //        return false;

    //    int axis;
    //    if (min.X == max.X)
    //        axis = 0;
    //    else if (min.Y == max.Y)
    //        axis = 1;
    //    else if (min.Z == max.Z)
    //        axis = 2;
    //    else
    //    {
    //        // In a 3D selection, we just check the position.
    //        return min.X <= pos.X && pos.X < max.X
    //            && min.Y <= pos.Y && pos.Y < max.Y
    //            && min.Z <= pos.Z && pos.Z < max.Z;
    //    }

    //    Vector3I normal = new Vector3I();
    //    normal[axis] = 1;
    //    Direction positive = Directions.FromAxis(axis, false);
    //    Direction negative = Directions.FromAxis(axis, true);

    //    // Check if we're aligned to the plane.
    //    if ((face == negative && pos[axis] == min[axis]) || (face == positive && pos[axis] == min[axis]))
    //    {
    //        // Check the other axes using the position as usual.
    //        for (int i = 0; i <= 2; i++)
    //        {
    //            if (axis != i && !(min[i] <= pos[i] && pos[i] < max[i]))
    //                return false;
    //        }
    //        return true;
    //    }
    //    return false;
    //}

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
