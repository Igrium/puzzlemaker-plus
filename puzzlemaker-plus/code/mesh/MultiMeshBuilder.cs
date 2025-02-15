using System.Collections.Generic;
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// A mesh builder that creates multiple surfaces based on quad materials.
/// </summary>
public class MultiMeshBuilder : IMeshBuilder
{
    private readonly Dictionary<int, MeshBuilder> _builders = new();

    public void AddQuad(in Quad quad)
    {
        int index = quad.MaterialIndex;
        MeshBuilder? builder = _builders.GetValueOrDefault(index);

        if (builder == null)
        {
            builder = new MeshBuilder();
            _builders[index] = builder;
        }

        builder.AddQuad(quad);
    }

    public void ToMesh(ArrayMesh mesh, params Material[] materials)
    {
        foreach (var (index, builder) in _builders)
        {
            Material? mat = index < materials.Length ? materials[index] : null;
            builder.ToMesh(mesh, mat);
        }
    }
}
