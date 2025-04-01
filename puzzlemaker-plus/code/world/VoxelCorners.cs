using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using Godot;

namespace PuzzlemakerPlus;

public enum VoxelCorner : byte
{
    None = 0,
    NNN = 1,
    NNP = 2,
    PNN = 4,
    PNP = 8,
    NPN = 16,
    NPP = 32,
    PPN = 64,
    PPP = 128,
    All = 255
}

/// <summary>
/// Can identify the normals for any given corner in the voxel mesh.
/// </summary>
public static class VoxelCorners
{
    public static VoxelCorner GetCorner(IVoxelView<PuzzlemakerVoxel> view, Vector3I vertex)
    {
        VoxelCorner corner = VoxelCorner.None;
        Vector3I pos = default;
        
        for (pos.X = -1; pos.X < 1; pos.X++)
        {
            for (pos.Y = -1; pos.Y < 1; pos.Y++)
            {
                for (pos.Z = -1; pos.Z < 1; pos.Z++)
                {
                    if (view.GetVoxel(vertex + pos).IsOpen)
                        corner |= Single(pos);
                }
            }
        }

        return corner;
    }

    public static VoxelCorner Single(Vector3I voxel)
    {
        if (voxel == new Vector3I(-1, -1, -1))
            return VoxelCorner.NNN;
        else if (voxel == new Vector3I(-1, -1, 0))
            return VoxelCorner.NNP;
        else if (voxel == new Vector3I(0, -1, -1))
            return VoxelCorner.PNN;
        else if (voxel == new Vector3I(0, -1, 0))
            return VoxelCorner.PNP;
        else if (voxel == new Vector3I(-1, 0, -1))
            return VoxelCorner.NPN;
        else if (voxel == new Vector3I(-1, 0, 0))
            return VoxelCorner.NPP;
        else if (voxel == new Vector3I(0, 0, -1))
            return VoxelCorner.PPN;
        else if (voxel == new Vector3I(0, 0, 0))
            return VoxelCorner.PPP;
        else
            return VoxelCorner.None;
    }

    public static bool HasVoxel(this VoxelCorner corner, Vector3I voxel)
    {
        if (voxel == new Vector3I(-1, -1, -1))
            return corner.HasFlag(VoxelCorner.NNN);
        else if (voxel == new Vector3I(-1, -1, 0))
            return corner.HasFlag(VoxelCorner.NNP);
        else if (voxel == new Vector3I(0, -1, -1))
            return corner.HasFlag(VoxelCorner.PNN);
        else if (voxel == new Vector3I(0, -1, 0))
            return corner.HasFlag(VoxelCorner.PNP);
        else if (voxel == new Vector3I(-1, 0, -1))
            return corner.HasFlag(VoxelCorner.NPN);
        else if (voxel == new Vector3I(-1, 0, 0))
            return corner.HasFlag(VoxelCorner.NPP);
        else if (voxel == new Vector3I(0, 0, -1))
            return corner.HasFlag(VoxelCorner.PPN);
        else if (voxel == new Vector3I(0, 0, 0))
            return corner.HasFlag(VoxelCorner.PPP);
        else
            return false;
    }

    public static IEnumerable<Vector3I> GetVoxels(this VoxelCorner corner)
    {
        Vector3I pos = default;
        for (pos.X = -1; pos.X < 1; pos.X++)
        {
            for (pos.Y = -1; pos.Y < 1; pos.Y++)
            {
                for (pos.X = -1; pos.X < 1; pos.X++)
                {
                    if (HasVoxel(corner, pos))
                        yield return pos;
                }
            }
        }
    }

    private static Vector3[] _normalLut;
    private static DirectionFlags[] _visibleEdgesLut;
    private static Vector3[][] _extrusionNormalLut;

    static VoxelCorners()
    {
        _normalLut = new Vector3[byte.MaxValue];
        _visibleEdgesLut = new DirectionFlags[byte.MaxValue];
        _extrusionNormalLut = new Vector3[byte.MaxValue][];

        List<Quad> quadCache = new List<Quad>(12);
        for (byte i = 0; i < byte.MaxValue; i++)
        {
            quadCache.Clear();
            VoxelCorner corner = (VoxelCorner)i;
            (_normalLut[i], _visibleEdgesLut[i]) = ComputeCornerData(corner);
            _extrusionNormalLut[i] = ComputeExtrusionNormals(corner).ToArray();
        }

    }

    private static (Vector3, DirectionFlags) ComputeCornerData(VoxelCorner corner)
    {
        Vector3 result = Vector3.Zero;
        if (corner == VoxelCorner.None || corner == VoxelCorner.All)
            return (result, default);

        byte[] mask = new byte[4];

        DirectionFlags edgeDirections = default;

        Vector3I pos = Vector3I.Zero;
        for (pos.Z = -1; pos.Z < 1; pos.Z++)
        {
            for (pos.Y = -1; pos.Y < 1; pos.Y++)
            {
                for (pos.X = -1; pos.X < 1; pos.X++)
                {
                    if (!corner.HasVoxel(pos))
                    {
                        Vector3 center = pos + new Vector3(.5f, .5f, .5f);
                        result += center;
                    }
                }
            }
        }

        // Exteremly simplified meshing algorithm to approximate desired normal
        for (int axis = 0; axis < 3; axis++)
        {
            Array.Fill(mask, default);
            int uAxis = (axis + 1) % 3;
            int vAxis = (axis + 2) % 3;
            pos = Vector3I.Zero;

            Vector3I normal = Vector3I.Zero;
            normal[axis] = -1;

            Vector3I du = Vector3I.Zero;
            du[uAxis] = 1;

            Vector3I dv = Vector3I.Zero;
            dv[vAxis] = 1;

            int index = 0;
            for (pos[vAxis] = -1; pos[vAxis] < 1; pos[vAxis]++)
            {
                for (pos[uAxis] = -1; pos[uAxis] < 1; pos[uAxis]++)
                {
                    bool current = corner.HasVoxel(pos);
                    bool compare = corner.HasVoxel(pos + normal);

                    if (current && !compare)
                    {
                        //result += normal;
                        mask[index] = 1;
                    }
                    else if (!current && compare)
                    {
                        //result -= normal;
                        mask[index] = 1;
                    }
                    index++;
                }
            }

            if (mask[0] != mask[1])
            {
                edgeDirections |= DirectionFlagsExtensions.AsFlag(Directions.FromAxis(vAxis, true));
            }
            if (mask[0] != mask[2])
            {
                edgeDirections |= DirectionFlagsExtensions.AsFlag(Directions.FromAxis(uAxis, true));
            }
            if (mask[2] != mask[3])
            {
                edgeDirections |= DirectionFlagsExtensions.AsFlag(Directions.FromAxis(vAxis, false));
            }
            if (mask[1] != mask[3])
            {
                edgeDirections |= DirectionFlagsExtensions.AsFlag(Directions.FromAxis(uAxis, false));
            }
        }
        return (result.Normalized(), edgeDirections);
    }

    public static Vector3 GetCornerNormal(this VoxelCorner corner)
    {
        return _normalLut[(byte)corner];
    }

    public static Vector3 GetCornerNormal(IVoxelView<PuzzlemakerVoxel> world, Vector3I vertex)
    {
        return _normalLut[(byte)GetCorner(world, vertex)];
    }

    private static void FloodFill(ref VoxelCorner filled, VoxelCorner corner, Vector3I pos)
    {
        filled |= Single(pos);
        Vector3I opposite = new Vector3I(pos.X == -1 ? 0 : -1, pos.Y == -1 ? 0 : -1, pos.Z == -1 ? 0 : -1);

        Vector3I compare = new Vector3I(opposite.X, pos.Y, pos.Z);
        if (corner.HasVoxel(compare) && !filled.HasVoxel(compare))
            FloodFill(ref filled, corner, compare);

        compare = new Vector3I(pos.X, opposite.Y, pos.Z);
        if (corner.HasVoxel(compare) && !filled.HasVoxel(compare))
            FloodFill(ref filled, corner, compare);

        compare = new Vector3I(pos.X, pos.Y, opposite.Z);
        if (corner.HasVoxel(compare) && !filled.HasVoxel(compare))
            FloodFill(ref filled, corner, compare);
    }

    private static IEnumerable<Vector3> ComputeExtrusionNormals(VoxelCorner corner)
    {
        VoxelCorner saturatedVoxels = VoxelCorner.None;

        Vector3I pos = default;
        for (pos.X = -1; pos.X < 1; pos.X++)
        {
            for (pos.Y = -1; pos.Y < 1; pos.Y++)
            {
                for (pos.Z = -1; pos.Z < 1; pos.Z++)
                {
                    if (corner.HasVoxel(pos) && !saturatedVoxels.HasVoxel(pos))
                    {
                        VoxelCorner filled = VoxelCorner.None;
                        FloodFill(ref filled, corner, pos);

                        if (filled == VoxelCorner.None)
                            continue;

                        saturatedVoxels |= filled;
                        yield return GetCornerNormal(filled);
                    }
                }
            }
        }


        //Vector3I pos = default;
        //for (pos.X = -1; pos.X < 1; pos.X++)
        //{
        //    for (pos.Y = -1; pos.Y < 1; pos.Y++)
        //    {
        //        for (pos.Z = -1; pos.Z < 1; pos.Z++)
        //        {
        //            Vector3I opposite = new Vector3I(pos.X == -1 ? 0 : -1, pos.Y == -1 ? 0 : -1, pos.Z == -1 ? 0 : -1);

        //            if (corner.HasVoxel(pos))
        //            {
        //                // If all connecting voxels match each other, we have a corner.
        //                if (!corner.HasVoxel(new Vector3I(opposite.X, pos.Y, pos.Z))
        //                    && !corner.HasVoxel(new Vector3I(pos.X, opposite.Y, pos.Z))
        //                    && !corner.HasVoxel(new Vector3I(pos.X, pos.Y, opposite.Z)))
        //                {
        //                    Vector3 center = pos + new Vector3(0.5f, 0.5f, 0.5f);
        //                    yield return -center.Normalized();
        //                }
        //            }
        //            else
        //            {
        //                // If all connecting voxels match each other, we have a corner.
        //                if (corner.HasVoxel(new Vector3I(opposite.X, pos.Y, pos.Z))
        //                    && !corner.HasVoxel(new Vector3I(pos.X, opposite.Y, pos.Z))
        //                    && !corner.HasVoxel(new Vector3I(pos.X, pos.Y, opposite.Z)))
        //                {
        //                    Vector3 center = pos + new Vector3(0.5f, 0.5f, 0.5f);
        //                    yield return center.Normalized();
        //                }
        //            }

        //        }
        //    }
        //}
    }

    public static Vector3[] GetExtrusionNormals(this VoxelCorner corner)
    {
        return _extrusionNormalLut[(byte)corner];
    }

    public static Vector3[] GetExtrusionNormals(IVoxelView<PuzzlemakerVoxel> world, Vector3I vertex)
    {
        return _extrusionNormalLut[(byte)GetCorner(world, vertex)];
    }

    public static DirectionFlags GetVisibleEdges(this VoxelCorner corner)
    {
        return _visibleEdgesLut[(byte)corner];
    }

    /// <summary>
    /// Given a vertex, figure out which directions will have a "visible edge"
    /// </summary>
    /// <param name="world">The voxel world.</param>
    /// <param name="vertex">Vertex to use.</param>
    /// <returns>Which directions have a visible edge.</returns>
    /// <remarks>A "visible edge" is defined as an edge creates a visible change in geometry at this vertex.
    /// If you would to turn off textures, you would be able to see it.</remarks>
    public static DirectionFlags GetVisibleEdges(IVoxelView<PuzzlemakerVoxel> world, Vector3I vertex)
    {
        return _visibleEdgesLut[(byte)GetCorner(world, vertex)];
    }
}
