using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// Creates a PolygonShape3D out of a set of quads.
/// </summary>
public class PolygonShapeBuilder
{
    private readonly List<Vector3> _verts = new();

    public void AddQuad(in Quad quad)
    {
        _verts.Add(quad.Vert1);
        _verts.Add(quad.Vert2);
        _verts.Add(quad.Vert3);

        _verts.Add(quad.Vert1);
        _verts.Add(quad.Vert3);
        _verts.Add(quad.Vert4);
    }

    public void AddQuads(IEnumerable<Quad> quads)
    {
        foreach (var quad in quads)
        {
            AddQuad(in quad);
        }
    }

    public void ToShape(ConcavePolygonShape3D shape)
    {
        if (!_verts.Any())
            return;

        shape.SetFaces(_verts.ToArray());
    }
}
