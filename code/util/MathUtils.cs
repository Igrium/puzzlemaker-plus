using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus;

public static class MathUtils
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
}
