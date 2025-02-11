using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// Represents a single face of a voxel.
/// </summary>
public struct VoxelFace
{
    public Vector3I Position;
    public Direction Direction;

    public VoxelFace(Vector3I position, Direction direction)
    {
        this.Position = position;
        this.Direction = direction;
    }

    public readonly VoxelFace WithPosition(Vector3I position)
    {
        VoxelFace result = this;
        result.Position = position;
        return result;
    }

    public readonly VoxelFace WithDirection(Direction direction)
    {
        VoxelFace result = this;
        result.Direction = direction;
        return result;
    }

    public readonly VoxelFace Opposite()
    {
        return new VoxelFace(Position + Direction.GetNormal(), Direction.Opposite());
    }

    /// <summary>
    /// Get the vertices of this voxel face.
    /// </summary>
    /// <returns>Face vertices in clockwise order looking at the voxel.</returns>
    public readonly (Vector3I, Vector3I, Vector3I, Vector3I) GetVertices()
    {
        switch (Direction)
        {
            case Direction.Up:
                return (
                    Position + new Vector3I(0, 1, 0),
                    Position + new Vector3I(1, 1, 0),
                    Position + new Vector3I(1, 1, 1),
                    Position + new Vector3I(0, 1, 1)
                );
            case Direction.Down:
                return (
                    Position + new Vector3I(0, 0, 0),
                    Position + new Vector3I(1, 0, 0),
                    Position + new Vector3I(1, 0, 1),
                    Position + new Vector3I(0, 0, 1)
                );
            case Direction.Left:
                return (
                    Position + new Vector3I(0, 0, 0),
                    Position + new Vector3I(0, 1, 0),
                    Position + new Vector3I(0, 1, 1),
                    Position + new Vector3I(0, 0, 1)
                );
            case Direction.Right:
                return (
                    Position + new Vector3I(1, 0, 0),
                    Position + new Vector3I(1, 1, 0),
                    Position + new Vector3I(1, 1, 1),
                    Position + new Vector3I(1, 0, 1)
                );
            case Direction.Forward:
                return (
                    Position + new Vector3I(0, 0, 1),
                    Position + new Vector3I(1, 0, 1),
                    Position + new Vector3I(1, 1, 1),
                    Position + new Vector3I(0, 1, 1)
                );
            case Direction.Back:
                return (
                    Position + new Vector3I(0, 0, 0),
                    Position + new Vector3I(1, 0, 0),
                    Position + new Vector3I(1, 1, 0),
                    Position + new Vector3I(0, 1, 0)
                );
            default:
                throw new ArgumentOutOfRangeException(nameof(Direction), Direction, null);
        }
    }
}
