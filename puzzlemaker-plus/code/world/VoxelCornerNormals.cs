using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// Can identify the normals for any given corner in the voxel mesh.
/// </summary>
public static class VoxelCornerNormals
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

    private static Vector3[] _lut;

    static VoxelCornerNormals()
    {
        _lut = new Vector3[byte.MaxValue];
        for (byte i = 0; i < byte.MaxValue; i++)
        {
            VoxelCorner corner = (VoxelCorner)i;
            _lut[i] = ComputeCornerNormal(corner);
        }
    }

    private static Vector3 ComputeCornerNormal(VoxelCorner corner)
    {
        Vector3 result = Vector3.Zero;
        if (corner == VoxelCorner.None || corner == VoxelCorner.All)
            return result;

        // Exteremly simplified meshing algorithm to approximate desired normal
        for (int axis = 0; axis < 3; axis++)
        {
            int uAxis = (axis + 1) % 3;
            int vAxis = (axis + 2) % 3;
            Vector3I pos = Vector3I.Zero;

            Vector3I normal = Vector3I.Zero;
            normal[axis] = -1;

            for (pos[vAxis] = -1; pos[vAxis] < 1; pos[vAxis]++)
            {
                for (pos[uAxis] = -1; pos[uAxis] < 1; pos[uAxis]++)
                {
                    bool current = corner.HasVoxel(pos);
                    bool compare = corner.HasVoxel(pos + normal);

                    if (current && !compare)
                    {
                        result += normal;
                    }
                    else if (!current && compare)
                    {
                        result -= normal;
                    }
                }
            }
        }

        return result.Normalized();
    }

    public static Vector3 ComputeCornerNormal(IVoxelView<PuzzlemakerVoxel> world, Vector3I vertex)
    {
        return _lut[(byte)GetCorner(world, vertex)];
    }

}
