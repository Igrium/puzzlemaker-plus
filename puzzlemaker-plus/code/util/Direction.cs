using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus;

[JsonConverter(typeof(DirectionJsonConverter))]
public enum Direction
{
    Up,
    Down,
    Left,
    Right,
    Forward,
    Back
}

public static class Directions
{
    /// <summary>
    /// Get the cardinal direction that a given normal vector is closest to.
    /// </summary>
    /// <param name="normal">The direction.</param>
    /// <returns>The closest direction.</returns>
    public static Direction GetClosestDirection(Vector3 normal)
    {
        float absX = MathF.Abs(normal.X);
        float absY = MathF.Abs(normal.Y);
        float absZ = MathF.Abs(normal.Z);

        if (absX > absY && absX > absZ)
        {
            return normal.X >= 0 ? Direction.Right : Direction.Left;
        }
        else if (absY > absX && absY > absZ)
        {
            return normal.Y >= 0 ? Direction.Up : Direction.Down;
        }
        else
        {
            return normal.Z >= 0 ? Direction.Back : Direction.Forward;
        }
    }

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

    public static int GetAxis(this Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
            case Direction.Down:
                return 1;
            case Direction.Left:
            case Direction.Right:
                return 0;
            case Direction.Forward:
            case Direction.Back:
                return 2;
            default: return 0;
        }
    }

    public static bool IsPositive(this Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
            case Direction.Right:
            case Direction.Back:
                return true;
            default:
                return false;
        }
    }

    public static Direction FromAxis(int axis, bool negative)
    {
        switch (axis)
        {
            case 0:
                return negative ? Direction.Right : Direction.Left;
            case 1:
                return negative ? Direction.Up : Direction.Down;
            case 2:
                return negative ? Direction.Back : Direction.Forward;
            default:
                throw new ArgumentOutOfRangeException(nameof(axis), "Axis must be 0 (x), 1 (y) or 2 (z).");
        }
    }

    public static Direction Opposite(this Direction direction)
    {
        switch (direction)
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
    /// Given a plane facing this Direction, 
    /// 
    /// get a vector determining the primary check Direction when performing a greedy mesh.
    /// </summary>
    /// <returns>Unit vector in the primary check Direction.</returns>
    public static Direction GetPerpendicularDir1(this Direction direction)
    {
        switch (direction)
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
        switch (direction)
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

    /// <summary>
    /// Get the axis and direction this direction is facing.
    /// </summary>
    /// <returns>The axis, in the form "X+", "X-", etc.</returns>
    public static string GetAxisString(this Direction direction)
    {
        switch (direction)
        {
            case Direction.Up: return "Y+";
            case Direction.Down: return "Y-";
            case Direction.Left: return "X-";
            case Direction.Right: return "X+";
            case Direction.Forward: return "Z-";
            case Direction.Back: return "Z+";
            default: return "";
        }
    }

    public static bool TryParseAxisString(string str, out Direction result)
    {
        result = default;
        str = str.Trim().ToUpper();
        if (str.Length != 2)
            return false;

        switch (str[0])
        {
            case 'Y':
                if (str[1] == '+')
                {
                    result = Direction.Up;
                    return true;
                }
                else if (str[1] == '-')
                {
                    result = Direction.Down;
                    return true;
                }
                break;
            case 'X':
                if (str[1] == '+')
                {
                    result = Direction.Right;
                    return true;
                }
                else if (str[1] == '-')
                {
                    result = Direction.Left;
                    return true;
                }
                break;
            case 'Z':
                if (str[1] == '+')
                {
                    result = Direction.Back;
                    return true;
                }
                else if (str[1] == '-')
                {
                    result = Direction.Forward;
                    return true;
                }
                break;
        }

        return false;
    }
}
