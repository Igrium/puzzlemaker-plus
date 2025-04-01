using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// A simple mesh quad.
/// Godot uses a clockwise winding-order, and this quad implementation uses the same.
/// </summary>
public record struct Quad
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

    public int MaterialIndex { get; set; }

    public Quad(Vector3 vert1, Vector3 vert2, Vector3 vert3, Vector3 vert4, int materialIndex = 0)
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

        this.MaterialIndex = materialIndex;
    }

    public Quad(Vector3 vert1, Vector3 vert2, Vector3 vert3, Vector3 vert4,
        Vector3 normal1, Vector3 normal2, Vector3 normal3, Vector3 normal4, int materialIndex = 0)
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

        this.MaterialIndex = materialIndex;
    }

    public Quad(Vector3 vert1, Vector3 vert2, Vector3 vert3, Vector3 vert4,
        Vector3 normal1, Vector3 normal2, Vector3 normal3, Vector3 normal4,
        Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4, int materialIndex)
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

        this.MaterialIndex = materialIndex;
    }

    public readonly Quad WithMaterialIndex(int materialIndex)
    {
        Quad result = this;
        result.MaterialIndex = materialIndex;
        return result;
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

    public readonly Vector3 ComputeFaceNormal()
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

    /// <summary>
    /// Construct an array with all these quad's edges.
    /// </summary>
    /// <returns>Array with all edges.</returns>
    public readonly Edge[] GetEdges()
    {
        Edge[] array = new Edge[4];
        array[0] = new Edge(Vert1, Vert2);
        array[1] = new Edge(Vert2, Vert3);
        array[2] = new Edge(Vert3, Vert4);
        array[3] = new Edge(Vert4, Vert1);
        return array;
    }

    public readonly void GetEdges(out Edge edge1, out Edge edge2, out Edge edge3, out Edge edge4)
    {
        edge1 = new Edge(Vert1, Vert2);
        edge2 = new Edge(Vert2, Vert3);
        edge3 = new Edge(Vert3, Vert4);
        edge4 = new Edge(Vert4, Vert1);
    }

    public readonly Edge GetEdge(int index)
    {
        switch (index)
        {
            case 0: return new Edge(Vert1, Vert2);
            case 1: return new Edge(Vert2, Vert3);
            case 2: return new Edge(Vert3, Vert4);
            case 3: return new Edge(Vert4, Vert1);
            default: throw new IndexOutOfRangeException("invalid edge: " + index);
        }
    }

    public readonly Vector3 GetEdgeTangent(int index)
    {
        switch (index)
        {
            case 0: return (Vert2 - Vert1).Normalized();
            case 1: return (Vert3 - Vert2).Normalized();
            case 2: return (Vert4 - Vert3).Normalized();
            case 3: return (Vert1 - Vert4).Normalized();
            default: throw new IndexOutOfRangeException("invalid edge: " + index);
        }
    }

    public readonly (int, int) GetEdgeVertIndices(int index)
    {
        switch (index)
        {
            case 0: return (0, 1);
            case 1: return (1, 2);
            case 2: return (2, 3);
            case 3: return (3, 0);
            default: throw new IndexOutOfRangeException("invalid edge: " + index);
        }
    }

    public Vector3 this[int i]
    {
        readonly get => GetVertex(i);
        set => SetVertex(i, in value);
    }

    public readonly Vector3 GetVertex(int index)
    {
        switch (index)
        {
            case 0: return Vert1;
            case 1: return Vert2;
            case 2: return Vert3;
            case 3: return Vert4;
            default: throw new IndexOutOfRangeException("invalid vertex " + index);
        }
    }

    public void SetVertex(int index, in Vector3 value)
    {
        switch (index)
        {
            case 0: Vert1 = value; break;
            case 1: Vert2 = value; break;
            case 2: Vert3 = value; break;
            case 3: Vert4 = value; break;
            default: throw new IndexOutOfRangeException("invalid vertex " + index);
        }
    }

    public readonly Vector3 GetNormal(int index)
    {
        switch (index)
        {
            case 0: return Normal1;
            case 1: return Normal2;
            case 2: return Normal3;
            case 3: return Normal4;
            default: throw new IndexOutOfRangeException("invalid vertex " + index);
        }
    }

    public void SetNormal(int index, in Vector3 value)
    {
        switch (index)
        {
            case 0: Normal1 = value; break;
            case 1: Normal2 = value; break;
            case 2: Normal3 = value; break;
            case 3: Normal4 = value; break;
            default: throw new IndexOutOfRangeException("invalid vertex " + index);
        }
    }

    public readonly Vector2 GetUV(int index)
    {
        switch (index)
        {
            case 0: return UV1;
            case 1: return UV2;
            case 2: return UV3;
            case 3: return UV4;
            default: throw new IndexOutOfRangeException("invalid vertex " + index);
        }
    }

    public void SetUV(int index, Vector2 value)
    {
        switch (index)
        {
            case 0: UV1 = value; break;
            case 1: UV2 = value; break;
            case 2: UV3 = value; break;
            case 3: UV4 = value; break;
            default: throw new IndexOutOfRangeException("invalid vertex " + index);
        }
    }

    public readonly Vector3 GetUniformVertNormal(int index)
    {
        Vector3 vert = this[index];
        Vector3 prev = this[(index + 4 - 1) % 4];
        Vector3 next = this[(index + 4 + 1) % 4];

        Vector3 tanPrev = (vert - prev).Normalized();
        Vector3 tanNext = (vert - next).Normalized();

        return (tanPrev + tanNext).Normalized();
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

    public static Quad operator *(Quad quad, float scalar)
    {
        quad.Vert1 *= scalar;
        quad.Vert2 *= scalar;
        quad.Vert3 *= scalar;
        quad.Vert4 *= scalar;
        return quad;
    }

    public static Quad operator *(float scalar, Quad quad) => quad * scalar;

    public override string ToString()
    {
        return $"Quad[{Vert1}, {Vert2}, {Vert3}, {Vert4} ({MaterialIndex})]";
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
