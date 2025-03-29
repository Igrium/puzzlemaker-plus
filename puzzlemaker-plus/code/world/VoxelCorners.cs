using System;
using System.Collections.Generic;
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// Can identify the normals for any given corner in the voxel mesh.
/// </summary>
public static class VoxelCorners
{

    private enum VoxelCorner : byte
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

    private static VoxelCorner GetCorner(IVoxelView<PuzzlemakerVoxel> view, Vector3I vertex)
    {
        VoxelCorner corner = VoxelCorner.None;
        if (view.GetVoxel(vertex + new Vector3I(-1, -1, -1)).IsOpen)
            corner |= VoxelCorner.NNN;
        if (view.GetVoxel(vertex + new Vector3I(-1, -1, 0)).IsOpen)
            corner |= VoxelCorner.NNP;
        if (view.GetVoxel(vertex + new Vector3I(0, -1, -1)).IsOpen)
            corner |= VoxelCorner.PNN;
        if (view.GetVoxel(vertex + new Vector3I(0, -1, 0)).IsOpen)
            corner |= VoxelCorner.PNP;
        if (view.GetVoxel(vertex + new Vector3I(-1, 0, -1)).IsOpen)
            corner |= VoxelCorner.NPN;
        if (view.GetVoxel(vertex + new Vector3I(-1, 0, 0)).IsOpen)
            corner |= VoxelCorner.NPP;
        if (view.GetVoxel(vertex + new Vector3I(0, 0, -1)).IsOpen)
            corner |= VoxelCorner.PPN;
        if (view.GetVoxel(vertex + new Vector3I(0, 0, 0)).IsOpen)
            corner |= VoxelCorner.PPP;
        return corner;
    }

    private static bool HasVoxel(this VoxelCorner corner, Vector3I voxel)
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

    private static Vector3[] _normalLut;
    private static DirectionFlags[] _visibleEdgesLut;

    static VoxelCorners()
    {
        _normalLut = new Vector3[byte.MaxValue];
        _visibleEdgesLut = new DirectionFlags[byte.MaxValue];

        List<Quad> quadCache = new List<Quad>(12);
        for (byte i = 0; i < byte.MaxValue; i++)
        {
            quadCache.Clear();
            VoxelCorner corner = (VoxelCorner)i;
            (_normalLut[i], _visibleEdgesLut[i]) = ComputeCornerData(corner);
        }
    }

    private static (Vector3, DirectionFlags) ComputeCornerData(VoxelCorner corner)
    {
        Vector3 result = Vector3.Zero;
        if (corner == VoxelCorner.None || corner == VoxelCorner.All)
            return (result, default);

        byte[] mask = new byte[4];

        DirectionFlags edgeDirections = default;

        // Exteremly simplified meshing algorithm to approximate desired normal
        for (int axis = 0; axis < 3; axis++)
        {
            Array.Fill(mask, default);
            int uAxis = (axis + 1) % 3;
            int vAxis = (axis + 2) % 3;
            Vector3I pos = Vector3I.Zero;

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
                        result += normal;
                        mask[index] = 1;
                    }
                    else if (!current && compare)
                    {
                        result -= normal;
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

    public static Vector3 GetCornerNormal(IVoxelView<PuzzlemakerVoxel> world, Vector3I vertex)
    {
        return _normalLut[(byte)GetCorner(world, vertex)];
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
