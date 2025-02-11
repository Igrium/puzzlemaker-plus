using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// A simple mesh quad.
/// Godot uses a clockwise winding-order, and this quad implementation uses the same.
/// </summary>
public struct Quad
{

    public static Quad Empty => new Quad(Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero);
    public Vector3 Vert1 { get; set; }
    public Vector3 Vert2 { get; set; }
    public Vector3 Vert3 { get; set; }
    public Vector3 Vert4 { get; set; }

    public Vector3 Normal1 { get; set; }
    public Vector3 Normal2 { get; set; }
    public Vector3 Normal3 { get; set; }
    public Vector3 Normal4 { get; set; }

    public Vector2 UV1 { get; set; }
    public Vector2 UV2 { get; set; }
    public Vector2 UV3 { get; set; }
    public Vector2 UV4 { get; set; }

    public Quad(Vector3 vert1, Vector3 vert2, Vector3 vert3, Vector3 vert4)
    {
        // if (AnyElementsEqual(vert1, vert2, vert3, vert4))
        // {
        //     GD.PushWarning($"Creating a quad with duplicate verts: ({vert1}, {vert2}, {vert3}, {vert4})");
        // }
        Vert1 = vert1;
        Vert2 = vert2;
        Vert3 = vert3;
        Vert4 = vert4;

        Normal1 = Vector3.Back;
        Normal2 = Vector3.Back;
        Normal3 = Vector3.Back;
        Normal4 = Vector3.Back;

        UV1 = Vector2.Zero;
        UV2 = Vector2.Zero;
        UV3 = Vector2.Zero;
        UV4 = Vector2.Zero;
    }

    public Quad(Vector3 vert1, Vector3 vert2, Vector3 vert3, Vector3 vert4,
        Vector3 normal1, Vector3 normal2, Vector3 normal3, Vector3 normal4)
    {
        Vert1 = vert1;
        Vert2 = vert2;
        Vert3 = vert3;
        Vert4 = vert4;

        Normal1 = normal1;
        Normal2 = normal2;
        Normal3 = normal3;
        Normal4 = normal4;

        UV1 = Vector2.Zero;
        UV2 = Vector2.Zero;
        UV3 = Vector2.Zero;
        UV4 = Vector2.Zero;
    }

    public Quad(Vector3 vert1, Vector3 vert2, Vector3 vert3, Vector3 vert4,
        Vector3 normal1, Vector3 normal2, Vector3 normal3, Vector3 normal4,
        Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
    {
        Vert1 = vert1;
        Vert2 = vert2;
        Vert3 = vert3;
        Vert4 = vert4;

        Normal1 = normal1;
        Normal2 = normal2;
        Normal3 = normal3;
        Normal4 = normal4;

        UV1 = uv1;
        UV2 = uv2;
        UV3 = uv3;
        UV4 = uv4;
    }

    public void FillUVs()
    {
        UV1 = new Vector2(0, 0);
        UV2 = new Vector2(1, 0);
        UV3 = new Vector2(1, 1);
        UV4 = new Vector2(0, 1);
    }

    public readonly Quad FilledUVs()
    {
        Quad result = this;
        result.FillUVs();
        return result;
    }

    /// <summary>
    /// Reset this quad's normals so that they point in the Direction of the face.
    /// </summary>
    public void ResetNormals()
    {
        Vector3 normal = ComputeFaceNormal();

        Normal1 = normal;
        Normal2 = normal;
        Normal3 = normal;
        Normal4 = normal;
    }

    public readonly Quad WithResetNormals()
    {
        Quad result = this;
        result.ResetNormals();
        return result;
    }

    private readonly Vector3 ComputeFaceNormal()
    {
        // Calculate two edge vectors
        Vector3 edge1 = Vert2 - Vert1;
        Vector3 edge2 = Vert3 - Vert1;

        // Compute the cross product of the two edge vectors in reverse order
        Vector3 normal = edge2.Cross(edge1);

        // Normalize the resulting vector to get the unit normal
        return normal.Normalized();
    }

    public readonly Quad Flipped()
    {
        Quad result = this;
        result.Vert1 = Vert4;
        result.Vert2 = Vert3;
        result.Vert3 = Vert2;
        result.Vert4 = Vert1;

        result.Normal1 = -Normal4;
        result.Normal2 = -Normal3;
        result.Normal3 = -Normal2;
        result.Normal4 = -Normal1;

        result.UV1 = UV4;
        result.UV2 = UV3;
        result.UV3 = UV2;
        result.UV4 = UV1;

        return result;
    }

    public static Quad operator +(Quad quad, in Vector3 vec)
    {
        quad.Vert1 += vec;
        quad.Vert2 += vec;
        quad.Vert3 += vec;
        quad.Vert4 += vec;
        return quad;
    }

    public static Quad operator +(Quad quad, in Vector3I vec)
    {
        quad.Vert1 += vec;
        quad.Vert2 += vec;
        quad.Vert3 += vec;
        quad.Vert4 += vec;
        return quad;
    }

    public static Quad operator -(Quad quad, in Vector3 vec) => quad + -vec;


    public static Quad operator -(Quad quad, in Vector3I vec) => quad + -vec;

    public static bool operator ==(Quad quad1, Quad quad2)
    {
        return quad1.Equals(quad2);
    }

    public static bool operator !=(Quad quad1, Quad quad2) => !(quad1 == quad2);

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Quad other && Equals(other);
    }
    public readonly bool Equals(Quad other)
    {
        return this.Vert1 == other.Vert1
            && this.Vert2 == other.Vert2
            && this.Vert3 == other.Vert3
            && this.Vert4 == other.Vert4
            && this.Normal1 == other.Normal1
            && this.Normal2 == other.Normal2
            && this.Normal3 == other.Normal3
            && this.Normal4 == other.Normal4
            && this.UV1 == other.UV1
            && this.UV2 == other.UV2
            && this.UV3 == other.UV3
            && this.UV4 == other.UV4;
    }

    public override int GetHashCode()
    {
        return (HashCode.Combine(Vert1, Vert2, Vert3, Vert4) * 31
            + HashCode.Combine(Normal1, Normal2, Normal3, Normal4)) * 31
            + HashCode.Combine(UV1, UV2, UV3, UV4);
    }

    public override string ToString()
    {
        return $"Quad[{Vert1}, {Vert2}, {Vert3}, {Vert4}]";
    }



    private static bool AnyElementsEqual<T>(params T[] elements)
    {
        if (elements.Length == 0 || elements.Length == 1) return false;
        for (int i = 0; i < elements.Length - 1; i++)
        {
            for (int j = i + 1; j < elements.Length; j++)
            {
                if (!EqualityComparer<T>.Default.Equals(elements[i], elements[j]))
                    return true;
            }
        }
        return false;
    }
}
