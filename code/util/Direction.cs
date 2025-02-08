using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus;

public enum Direction
{
    Up,
    Down,
    Left,
    Right,
    Forward,
    Back
}

public static class DirectionMethods
{
    public static Vector3I GetNormal(this Direction direction)
    {
        switch (direction)
        {
            case Direction.Up: return Vector3I.Up;
            case Direction.Down: return Vector3I.Down;
            case Direction.Left: return Vector3I.Left;
            case Direction.Right: return Vector3I.Right;
            case Direction.Forward: return Vector3I.Forward;
            case Direction.Back: return Vector3I.Back;
            default: return Vector3I.Zero;
        }
    }

    public static Direction Opposite(this Direction direction)
    {
        switch(direction)
        {
            case Direction.Up: return Direction.Down;
            case Direction.Down: return Direction.Up;
            case Direction.Left: return Direction.Right;
            case Direction.Right: return Direction.Left;
            case Direction.Forward: return Direction.Back;
            case Direction.Back: return Direction.Forward;
            default: return Direction.Up;
        }
    }

    /// <summary>
    /// Given a plane facing this direction, 
    /// get a vector determining the primary check direction when performing a greedy mesh.
    /// </summary>
    /// <returns>Unit vector in the primary check direction.</returns>
    public static Direction GetPerpendicularDir1(this Direction direction)
    {
        switch(direction)
        {
            case Direction.Up: return Direction.Right;
            case Direction.Down: return Direction.Right;
            case Direction.Left: return Direction.Forward;
            case Direction.Right: return Direction.Forward;
            case Direction.Forward: return Direction.Right;
            case Direction.Back: return Direction.Right;
            default: return Direction.Right;
        }
    }

    public static Direction GetPerpendicularDir2(this Direction direction)
    {
        switch(direction)
        {
            case Direction.Up: return Direction.Forward;
            case Direction.Down: return Direction.Forward;
            case Direction.Left: return Direction.Up;
            case Direction.Right: return Direction.Up;
            case Direction.Forward: return Direction.Up;
            case Direction.Back: return Direction.Up;
            default: return Direction.Forward;
        }
    }
}
