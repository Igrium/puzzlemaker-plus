using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PuzzlemakerPlus;

public static class MeshUtils
{
    /// <summary>
    /// Compute the cavity of an edge in a quad mesh.
    /// </summary>
    /// <param name="mesh">The mesh to look in.</param>
    /// <param name="edge">The edge in question.</param>
    /// <returns>The angle in radians. -1 if the edge could not be found or it lacks at least 2 faces.</returns>
    public static float GetEdgeCavity(SimpleQuadMeshOld mesh, in Edge edge)
    {
        Quad[] quads = mesh.GetQuads(in edge).ToArray();
        if (quads.Length < 2)
            return -1;

        return quads[0].ComputeFaceNormal().AngleTo(quads[1].ComputeFaceNormal());
    }

    /// <summary>
    /// Get all the edges in a mesh that have a cavity within a specific range.
    /// </summary>
    /// <param name="mesh">Mesh in question.</param>
    /// <param name="minAngle">Minimum angle in radians.</param>
    /// <param name="maxAngle">Maximum angle in radians.</param>
    /// <returns>Enumerable with edges.</returns>
    public static IEnumerable<Edge> FilterEdges(this SimpleQuadMeshOld mesh, float minAngle, float maxAngle)
    {
        foreach (var (edge, quadIndices) in mesh.EdgeCache)
        {
            Quad[] quads = quadIndices.Select(i => mesh.Quads[i]).ToArray();
            if (quads.Length < 2)
                continue;

            float angle = quads[0].ComputeFaceNormal().AngleTo(quads[1].ComputeFaceNormal());
            if (minAngle <= angle && angle <= maxAngle)
                yield return edge;
        }
    }
}
