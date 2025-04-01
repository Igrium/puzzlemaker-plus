using System;
using System.Reflection.Metadata.Ecma335;
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// Represents one of 12 edges in a cube.
/// </summary>
public enum EdgeDirection
{
    DownFront,
    DownRight,
    DownBack,
    DownLeft,
    FrontRight,
    BackRight,
    BackLeft,
    FrontLeft,
    TopFront,
    TopRight,
    TopBack,
    TopLeft
}

public static class EdgeDirections
{
    public static (Direction, Direction) GetFaceDirections(this EdgeDirection edgeDir)
    {
        switch (edgeDir)
        {
            case EdgeDirection.DownFront: return (Direction.Down, Direction.Forward);
            case EdgeDirection.DownRight: return (Direction.Down, Direction.Right);
            case EdgeDirection.DownBack: return (Direction.Down, Direction.Back);
            case EdgeDirection.DownLeft: return (Direction.Down, Direction.Left);
            case EdgeDirection.FrontRight: return (Direction.Forward, Direction.Right);
            case EdgeDirection.BackRight: return (Direction.Back, Direction.Right);
            case EdgeDirection.BackLeft: return (Direction.Back, Direction.Left);
            case EdgeDirection.FrontLeft: return (Direction.Forward, Direction.Left);
            case EdgeDirection.TopFront: return (Direction.Up, Direction.Forward);
            case EdgeDirection.TopRight: return (Direction.Up, Direction.Right);
            case EdgeDirection.TopBack: return (Direction.Up, Direction.Back);
            case EdgeDirection.TopLeft: return (Direction.Up, Direction.Left);
            default: throw new ArgumentOutOfRangeException(nameof(edgeDir), edgeDir, null);
        }
    }

    public static Vector3 GetNormal(this EdgeDirection edgeDirection)
    {
        // This could be pre-calculated, but I don't care.
        var (dir1, dir2) = GetFaceDirections(edgeDirection);
        return ((Vector3)dir1.GetNormal() + dir2.GetNormal()).Normalized();
    }

    /// <summary>
    /// Get the edge direction between two faces.
    /// </summary>
    /// <param name="face1">Direction of face 1</param>
    /// <param name="face2">Direction of face 2</param>
    /// <returns>The edge, or <c>null</c> if no edge exists between those faces.</returns>
    public static EdgeDirection? FromFaces(Direction face1, Direction face2)
    {
        bool hasFace(Direction face)
        {
            return face1 == face || face2 == face;
        }

        if (face1 == face2)
            return null;

        if (hasFace(Direction.Down))
        {
            if (hasFace(Direction.Forward))
                return EdgeDirection.DownFront;
            else if (hasFace(Direction.Right))
                return EdgeDirection.DownRight;
            else if (hasFace(Direction.Back))
                return EdgeDirection.DownBack;
            else if (hasFace(Direction.Left))
                return EdgeDirection.DownLeft;
        }
        else if (hasFace(Direction.Up))
        {
            if (hasFace(Direction.Forward))
                return EdgeDirection.TopFront;
            else if (hasFace(Direction.Right))
                return EdgeDirection.TopRight;
            else if (hasFace(Direction.Back))
                return EdgeDirection.TopBack;
            else if (hasFace(Direction.Left))
                return EdgeDirection.TopLeft;
        }
        else if (hasFace(Direction.Forward))
        {
            if (hasFace(Direction.Left))
                return EdgeDirection.FrontLeft;
            else if (hasFace(Direction.Right))
                return EdgeDirection.FrontRight;
        }
        else if (hasFace(Direction.Back))
        {
            if (hasFace(Direction.Left))
                return EdgeDirection.BackLeft;
            else if (hasFace(Direction.Right))
                return EdgeDirection.BackRight;
        }
        return null;
    }
}
