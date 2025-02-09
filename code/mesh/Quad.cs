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
    /// Reset this quad's normals so that they point in the direction of the face.
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

    public static Quad operator -(Quad quad, in Vector3 vec)
    {
        return quad + -vec;
    }

    public static Quad operator -(Quad quad, in Vector3I vec)
    {
        return quad + -vec;
    }
}
