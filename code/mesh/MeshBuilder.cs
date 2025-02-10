
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PuzzlemakerPlus;

public class MeshBuilder
{
    private readonly List<Vector3> _vertices = new();
    private readonly List<Vector3> _normals = new();
    private readonly List<Vector2> _uvs = new();
    private readonly List<int> _indices = new();

    private int _numVerts = 0;

    public void AddQuad(in Quad quad)
    {
        _vertices.Add(quad.Vert1);
        _vertices.Add(quad.Vert2);
        _vertices.Add(quad.Vert3);
        _vertices.Add(quad.Vert4);

        _normals.Add(quad.Normal1);
        _normals.Add(quad.Normal2);
        _normals.Add(quad.Normal3);
        _normals.Add(quad.Normal4);

        _uvs.Add(quad.UV1);
        _uvs.Add(quad.UV2);
        _uvs.Add(quad.UV3);
        _uvs.Add(quad.UV4);

        _indices.Add(_numVerts + 0);
        _indices.Add(_numVerts + 1);
        _indices.Add(_numVerts + 2);

        _indices.Add(_numVerts + 0);
        _indices.Add(_numVerts + 2);
        _indices.Add(_numVerts + 3);

        _numVerts += 4;
    }

    public void AddQuads(IEnumerable<Quad> quads)
    {
        foreach (var quad in quads)
        {
            AddQuad(in quad);
        }
    }

    public void ToMesh(ArrayMesh mesh, Material? material = null)
    {
        using (var gArray = new Array())
        {
            gArray.Resize((int)Mesh.ArrayType.Max);
            gArray[(int)Mesh.ArrayType.Vertex] = _vertices.ToArray();
            gArray[(int)Mesh.ArrayType.Normal] = _normals.ToArray();
            gArray[(int)Mesh.ArrayType.TexUV] = _uvs.ToArray();
            gArray[(int)Mesh.ArrayType.Index] = _indices.ToArray();

            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, gArray);

            if (material != null)
            {
                int surfaceIndex = mesh.GetSurfaceCount() - 1;
                mesh.SurfaceSetMaterial(surfaceIndex, material);
            }
        }
    }
}
