using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzlemakerPlus;

/// <summary>
/// A utility enum that represents possible directions as bit flags.
/// </summary>
[Flags]
public enum DirectionFlags : byte
{
    Up = 1,
    Down = 2,
    Left = 4,
    Right = 8,
    Forward = 16,
    Back = 32,
    All = Up | Down | Left | Right | Forward | Back
}

public static class DirectionFlagsExtensions
{
    public static DirectionFlags AsFlag(this Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:
                return DirectionFlags.Up;
            case Direction.Down:
                return DirectionFlags.Down;
            case Direction.Left:
                return DirectionFlags.Left;
            case Direction.Right:
                return DirectionFlags.Right;
            case Direction.Forward:
                return DirectionFlags.Forward;
            case Direction.Back:
                return DirectionFlags.Back;
            default:
                return 0;
        }
    }

    public static DirectionFlags FromDirections(IEnumerable<Direction> directions)
    {
        DirectionFlags flags = 0;
        foreach (var dir in directions)
        {
            flags |= dir.AsFlag();
        }
        return flags;
    }

    public static bool HasDirection(this DirectionFlags flags, Direction direction)
    {
        return flags.HasFlag(direction.AsFlag());
    }

    public static IEnumerable<Direction> GetDirections(this DirectionFlags flags)
    {
        if (flags.HasFlag(DirectionFlags.Up))
            yield return Direction.Up;
        if (flags.HasFlag(DirectionFlags.Down))
            yield return Direction.Down;
        if (flags.HasFlag(DirectionFlags.Left))
            yield return Direction.Left;
        if (flags.HasFlag(DirectionFlags.Right))
            yield return Direction.Right;
        if (flags.HasFlag(DirectionFlags.Forward))
            yield return Direction.Forward;
        if (flags.HasFlag(DirectionFlags.Back))
            yield return Direction.Back;
    }
}