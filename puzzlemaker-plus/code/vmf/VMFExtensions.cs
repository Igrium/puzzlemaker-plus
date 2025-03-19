using System;
using Godot;
using VMFLib.Objects;

namespace PuzzlemakerPlus.VMF;

internal static class VMFExtensions
{
    public static Vec3 ToVec3(this Vector3 vec) => new Vec3(vec.X, vec.Y, vec.Z);

    public static Vector3 ToVector3(this Vec3 vec) => new Vector3((float)vec.X, (float)vec.Y, (float)vec.Z);

    public static VMFLib.Objects.Plane ToPlane(this Quad quad)
    {
        return new VMFLib.Objects.Plane(quad.Vert1.ToVec3(), quad.Vert2.ToVec3(), quad.Vert3.ToVec3());
    }
}
