using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus;

[GlobalClass]
public partial class PuzzlemakerWorld : VoxelWorld<PuzzlemakerVoxel>
{   
    /// <summary>
    /// Draw a set of blocks from this world into a mesh.
    /// </summary>
    /// <param name="mesh">Mesh to add to.</param>
    /// <param name="chunk">Negative-most position of the chunk.</param>
    /// <param name="chunkSize">Size of the chunk.</param>
    /// <param name="invert">If set, render the inside of the blocks instead of the outside.</param>
    public void RenderChunk(ArrayMesh mesh, Vector3I chunk, int chunkSize = 16, bool invert = true) 
    {
        RenderChunkAndCollision(mesh, null, chunk, chunkSize, invert);
    }

    /// <summary>
    /// Create render and collision geometry from a set of blocks from this world.
    /// </summary>
    /// <param name="mesh">Mesh to add render geometry to.</param>
    /// <param name="collision">Shape to add collision geometry to.</param>
    /// <param name="chunk">Most-negative position of the chunk in world-space.</param>
    /// <param name="chunkSize">Size of the chunk in voxels.</param>
    /// <param name="invert">If set, render the inside of the blocks instead of the outside.</param>
    public void RenderChunkAndCollision(ArrayMesh? mesh, ConcavePolygonShape3D? collision, Vector3I chunk, int chunkSize = 16, bool invert = true)
    {
        Quad[] quads = GreedyMesh.DoGreedyMesh(this, chunkSize, chunk, invert: invert).ToArray();
        
        if (mesh != null)
        {
            MeshBuilder builder = new MeshBuilder();
            for (int i = 0; i < quads.Length; i++)
            {
                builder.AddQuad(in quads[i]);
            }
            builder.ToMesh(mesh);
        }

        if (collision != null)
        {
            PolygonShapeBuilder builder = new PolygonShapeBuilder();
            for (int i = 0; i < quads.Length; i++)
            {
                builder.AddQuad(in quads[i]);
            }
            builder.ToShape(collision);
        }
    }
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
        Open = 1,
        Up = 2,
        Down = 4,
        Left = 8,
        Right = 16,
        Front = 32,
        Back = 64
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
        //GD.Print((int)flag);
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
