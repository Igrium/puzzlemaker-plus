﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
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
    /// <param name="chunk">The chunk coordinate of the chunk to render.</param>
    /// <param name="invert">If set, render the inside of the blocks instead of the outside.</param>
    public void RenderChunk(ArrayMesh mesh, Vector3I chunk, bool invert = true) 
    {
        RenderChunkAndCollision(mesh, null, chunk, invert);
    }

    /// <summary>
    /// Create render and collision geometry from a set of blocks from this world.
    /// </summary>
    /// <param name="mesh">Mesh to add render geometry to.</param>
    /// <param name="collision">Shape to add collision geometry to.</param>
    /// <param name="chunk">The chunk coordinate of the chunk to render</param>
    /// <param name="invert">If set, render the inside of the blocks instead of the outside.</param>
    public void RenderChunkAndCollision(ArrayMesh? mesh, ConcavePolygonShape3D? collision, Vector3I chunk, bool invert = true)
    {
        Quad[] quads = new GreedyMesh().DoGreedyMesh(this, chunk, uvScale: .25f, invert: invert).ToArray();
        
        if (mesh != null)
        {
            MultiMeshBuilder builder = new();
            for (int i = 0; i < quads.Length; i++)
            {
                builder.AddQuad(in quads[i]);
            }
            builder.ToMesh(mesh, EditorState.Instance.Theme.EditorMaterials);
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
    // TODO: We need to find a way to store the subdivision on a per-face basis.
    [Flags]
    public enum VoxelFlags : byte
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
    /// The bit flags of this voxel. Contains _data about whether it's open and its portalability.
    /// </summary>
    public byte Flags;

    /// <summary>
    /// The subdivision level of the voxel. 0 = 128 hammer units; 1 = 64 hammer units; 2 = 32 hammer units.
    /// </summary>
    public byte Subdivision;

    public bool IsOpen 
    {
        readonly get => HasFlag(VoxelFlags.Open);
        set => SetFlag(VoxelFlags.Open, value);
    }
    public bool UpPortalable
    {
        readonly get => HasFlag(VoxelFlags.Up);
        set => SetFlag(VoxelFlags.Up, value);
    }
    public bool DownPortalable
    {
        readonly get => HasFlag(VoxelFlags.Down);
        set => SetFlag(VoxelFlags.Down, value);
    }
    public bool LeftPortalable
    {
        readonly get => HasFlag(VoxelFlags.Left);
        set => SetFlag(VoxelFlags.Left, value);
    }
    public bool RightPortalable
    {
        readonly get => HasFlag(VoxelFlags.Right);
        set => SetFlag(VoxelFlags.Right, value);
    }
    public bool FrontPortalable
    {
        readonly get => HasFlag(VoxelFlags.Front);
        set => SetFlag(VoxelFlags.Front, value);
    }
    public bool BackPortalable
    {
        readonly get => HasFlag(VoxelFlags.Back);
        set => SetFlag(VoxelFlags.Back, value);
    }

    public readonly PuzzlemakerVoxel WithOpen(bool open)
    {
        PuzzlemakerVoxel result = this;
        result.IsOpen = open;
        return result;
    }

    public readonly bool HasFlag(VoxelFlags flag)
    {
        return ((Flags & (byte)flag) == (byte)flag);
    }


    public void SetFlag(VoxelFlags flag)
    {
        this.Flags |= (byte)flag;
    }

    public void SetFlag(VoxelFlags flag, bool value)
    {
        if (value)
            SetFlag(flag);
        else
            ClearFlag(flag);
    }

    public readonly PuzzlemakerVoxel WithFlag(VoxelFlags flag)
    {
        PuzzlemakerVoxel result = this;
        result.SetFlag(flag);
        return result;
    }

    public void SetFlags(params VoxelFlags[] flags)
    {
        foreach (VoxelFlags flag in flags)
        {
            Flags |= (byte)flag;
        }
    }

    public readonly PuzzlemakerVoxel WithFlags(params VoxelFlags[] flags)
    {
        PuzzlemakerVoxel result = this;
        result.SetFlags(flags);
        return result;
    }

    public void ClearFlag(VoxelFlags flag)
    {
        Flags &= (byte)~flag;
    }

    public readonly PuzzlemakerVoxel WithoutFlag(VoxelFlags flag)
    {
        PuzzlemakerVoxel result = this;
        result.ClearFlag(flag);
        return result;
    }

    public void ClearFlags(params VoxelFlags[] flags)
    {
        foreach (VoxelFlags flag in flags)
        {
            Flags &= (byte)~flag;
        }
    }

    public readonly PuzzlemakerVoxel WithoutFlags(params VoxelFlags[] flags)
    {
        PuzzlemakerVoxel result = this;
        result.ClearFlags(flags);
        return result;
    }

    public PuzzlemakerVoxel WithSubdivision(byte subdivision)
    {
        PuzzlemakerVoxel result = this;
        result.Subdivision = subdivision;
        return result;
    }

    public readonly bool IsPortalable(Direction direction)
    {
        switch(direction)
        {
            case Direction.Up:
                return UpPortalable;
            case Direction.Down:
                return DownPortalable;
            case Direction.Left:
                return LeftPortalable;
            case Direction.Right: 
                return RightPortalable;
            case Direction.Forward:
                return FrontPortalable;
            case Direction.Back:
                return BackPortalable;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction));
        }
    }

    public void SetPortalability(Direction direction, bool value)
    {
        switch(direction)
        {
            case Direction.Up:
                UpPortalable = value;
                break;
            case Direction.Down:
                DownPortalable = value;
                break;
            case Direction.Left:
                LeftPortalable = value;
                break;
            case Direction.Right:
                RightPortalable = value;
                break;
            case Direction.Forward:
                FrontPortalable = value;
                break;
            case Direction.Back:
                BackPortalable = value;
                break;
        }
    }

    public readonly PuzzlemakerVoxel WithPortalability(Direction direction, bool value)
    {
        PuzzlemakerVoxel result = this;
        result.SetPortalability(direction, value);
        return result;
    }

    public readonly PuzzlemakerVoxel WithPortalability(Direction? direction, bool value)
    {
        PuzzlemakerVoxel result = this;
        if (direction.HasValue)
            result.SetPortalability(direction.Value, value);
        else
            result.SetPortalability(value);
        return result;
    }

    /// <summary>
    /// Set the portalability for all sides.
    /// </summary>
    /// <param name="value">Portalability</param>
    public void SetPortalability(bool value)
    {
        FrontPortalable = value;
        BackPortalable = value;
        UpPortalable = value;
        DownPortalable = value;
        LeftPortalable = value;
        RightPortalable = value;
    }

    /// <summary>
    /// Set the portalability for all sides.
    /// </summary>
    /// <param name="value">Portalability</param>
    /// <returns>A copy of this voxel with the set portalability.</returns>
    public readonly PuzzlemakerVoxel WithPortalability(bool value)
    {
        PuzzlemakerVoxel result = this;
        result.SetPortalability(value);
        return result;
    }
}
