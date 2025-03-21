
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// Conversions between godot and source coordinate spaces. In this case, 1 godot unit = 32 Source units.
/// </summary>
public static class CoordConverter
{
    public static Vector3 ToSourceVector(this Vector3 vec)
    {
        return new Vector3(vec.X, -vec.Z, vec.Y) * 32;
    }

    public static Vector3I ToSourceVector(this Vector3I vec)
    {
        return new Vector3I(vec.X, -vec.Z, vec.Y) * 32;
    }

    public static Vector3 ToSourceNoScale(this Vector3 vec)
    {
        return new Vector3(vec.X, -vec.Z, vec.Y);
    }

    public static Vector3 ToGodotVector(this Vector3 vec)
    {
        return new Vector3(vec.X, vec.Z, -vec.Y) / 32;
    }

    public static Vector3I ToGodotVector(this Vector3I vec)
    {
        return new Vector3I(vec.X, vec.Z, -vec.Y) / 32;
    }

    public static Quad ToSourceQuad(in this Quad quad)
    {
        Quad result = new();
        result.Vert1 = quad.Vert1.ToSourceVector();
        result.Vert2 = quad.Vert2.ToSourceVector();
        result.Vert3 = quad.Vert3.ToSourceVector();
        result.Vert4 = quad.Vert4.ToSourceVector();

        result.Normal1 = quad.Normal1.ToSourceNoScale();
        result.Normal2 = quad.Normal2.ToSourceNoScale();
        result.Normal3 = quad.Normal3.ToSourceNoScale();
        result.Normal4 = quad.Normal4.ToSourceNoScale();

        result.UV1 = quad.UV1;
        result.UV2 = quad.UV2;
        result.UV3 = quad.UV3;
        result.UV4 = quad.UV4;

        result.MaterialIndex = quad.MaterialIndex;
        return result;
    }

    public static Vector3 ToSourceEuler(this Vector3 eulerAngles)
    {
        return Quaternion.FromEuler(eulerAngles).ToSourceEuler();
    }

    // This math was a pain of trial and error to figure out lol.
    public static Quaternion ToSourceRotation(this Quaternion quat)
    {
        return new Quaternion(new Vector3(0, -1, 0), Mathf.Pi / 2) * quat * new Quaternion(new Vector3(0, 1, 0), Mathf.Pi);
    }

    public static Vector3 ToSourceEuler(this Quaternion quat)
    {
        return quat.ToSourceRotation().GetEuler(EulerOrder.Yxz);
    }
    public static Vector3 ToDegrees(this Vector3 eulerRadians)
    {
        return new Vector3(Mathf.RadToDeg(eulerRadians.X), Mathf.RadToDeg(eulerRadians.Y), Mathf.RadToDeg(eulerRadians.Z));
    }
}
