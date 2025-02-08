using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// Utility to simplify the construction of meshes with quads.
/// </summary>
public struct MeshConstructor
{
    private static readonly Vector2 topUV = new Vector2(0, 0);

    private List<Vector3> _vertices;
    private List<int> _indices;
    private List<Vector2> _uvs;

    private int _faceCount;

    public IReadOnlyList<Vector3> Vertices => _vertices;
    public IReadOnlyList<int> Indices => _indices;
    public IReadOnlyList<Vector2> UVs => _uvs;
    public int FaceCount => _faceCount;

    public void AddFace(in (Vector3, Vector3, Vector3, Vector3) vertices,
        in (Vector3, Vector3, Vector3, Vector3)? uvs)
    {
        
    }
}

/// <summary>
/// Simple struct to hold 4 values of a single type to reduce typing.
/// </summary>
/// <typeparam name="T">Type to hold.</typeparam>
public struct FourValues<T>
{
    public T Value1;
    public T Value2;
    public T Value3;
    public T Value4;

    public FourValues(T val1, T val2, T val3, T val4)
    {
        this.Value1 = val1; 
        this.Value2 = val2;
        this.Value3 = val3; 
        this.Value4 = val4;
    }
}