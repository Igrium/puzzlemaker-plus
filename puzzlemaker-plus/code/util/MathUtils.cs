using System;
using Godot;

namespace PuzzlemakerPlus;

internal static class MathUtils
{
    public static int Min(int int1, int int2, int int3, int int4)
    {
        return Math.Min(Math.Min(int1, int2), Math.Min(int3, int4));
    }

    public static int Max(int int1, int int2, int int3, int int4)
    {
        return Math.Max(Math.Max(int1, int2), Math.Max(int3, int4));
    }

    public static int RoundDown(int num, int factor)
    {
        if (num % factor == 0)
            return num;
        else
            return (int)MathF.Floor(num / (float)factor) * factor;
    }

    public static int RoundUp(int num, int factor)
    {
        if (num % factor == 0)
            return num;
        else
            return (int)MathF.Ceiling(num / (float)factor) * factor;
    }
    
    /// <summary>
    /// Round a vector to the nearest integer.
    /// </summary>
    public static Vector3I RoundInt(this in Vector3 vector)
    {
        return new Vector3I((int)MathF.Round(vector.X), (int)MathF.Round(vector.Y), (int)MathF.Round(vector.Z));
    }

    /// <summary>
    /// Check if an axis-aligned ray intersects with a bounding box (inclusive).
    /// </summary>
    /// <param name="start">Ray start position.</param>
    /// <param name="direction">Ray end position.</param>
    /// <param name="boundsMin">Bounding box min.</param>
    /// <param name="boundsMax">Bounding box max</param>
    /// <returns>The first intersected voxel, or null if none exists.</returns>
    /// <remarks>There's no check to make sure boundsMin is less than boundsMax. I'm not sure what will happen if it's not.</remarks>
    public static Vector3I? IntersectAxisAlignedRay(Vector3I start, Direction direction, Vector3I boundsMin, Vector3I boundsMax)
    {
        int axis = direction.GetAxis();

        // First check if the line intersects the bounds.
        for (int i = 0; i < 3; i++)
        {
            if (i == axis) continue;
            if (start[i] < boundsMin[i] || start[i] > boundsMax[i])
                return null;
        }

        // Check if the start position is within the bounds for the axis dimension
        if (boundsMin[axis] <= start[axis] && start[axis] <= boundsMax[axis])
            return start;

        Vector3I res = start;
        if (direction.IsPositive() && start[axis] < boundsMin[axis])
        {
            res[axis] = boundsMin[axis];
            return res;
        }
        else if (!direction.IsPositive() && boundsMax[axis] < start[axis])
        {
            res[axis] = boundsMax[axis];
            return res;
        }

        return null;
    }
}
