using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus;

internal static class VectorExtensions
{
    public static Vector3I RoundInt(this Vector3 vec)
    {
        vec = vec.Round();
        return new Vector3I((int)vec.X, (int)vec.Y, (int)vec.Z);
    }

    public static Vector3I CeilInt(this Vector3 vec)
    {
        vec = vec.Ceil();
        return new Vector3I((int)vec.X, (int)vec.Y, (int)vec.Z);
    }

    public static Vector3I FloorInt(this Vector3 vec)
    {
        vec = vec.Floor();
        return new Vector3I((int)vec.X, (int)vec.Y, (int)vec.Z);
    }

    public static Vector3I Truncate(this Vector3 vec)
    {
        return new Vector3I((int)vec.X, (int)vec.Y, (int)vec.Z);
    }
}
