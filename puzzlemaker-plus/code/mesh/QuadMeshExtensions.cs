using System;
using System.Collections;
using System.Collections.Generic;
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// Various extension methods that don't belong in the QuadMesh base class.
/// </summary>
public static class QuadMeshExtensions
{
    /// <summary>
    /// Get the angle between the faces joining an edge.
    /// </summary>
    /// <param name="edge">The edge.</param>
    /// <returns>The angle in radians. -1 if the edge was not found or didn't have two faces.</returns>
    /// <remarks>Assumes the mesh is manifold, and no edges have more than one quad.</remarks>
    public static float GetEdgeAngle(this SimpleQuadMesh quadMesh, Edge edge)
    {
        var (quads, numQuads) = quadMesh.GetAdjoiningFaces(edge).GetElements(2);
        if (numQuads < 2)
            return -1;

        Vector3 normal0 = quads[0].ComputeFaceNormal();
        Vector3 normal1 = quads[1].ComputeFaceNormal();

        return normal0.AngleTo(normal1);
    }

    /// <summary>
    /// Get an enumerable with all edges in the mesh with faces between the given angles.
    /// </summary>
    /// <param name="quadMesh">The mesh.</param>
    /// <param name="minAngle">Minimum angle in radians, inclusive.</param>
    /// <param name="maxAngle">Maximum angle in radians, inclusive.</param>
    /// <returns></returns>
    public static IEnumerable<Edge> GetEdgesWithAngle(this SimpleQuadMesh quadMesh, float minAngle, float maxAngle)
    {
        foreach (var (edge, refs) in quadMesh.EdgeRefCache)
        {
            if (refs.Count < 2) continue;

            Vector3 normal0 = quadMesh.Quads[refs[0].FaceIndex].ComputeFaceNormal();
            Vector3 normal1 = quadMesh.Quads[refs[1].FaceIndex].ComputeFaceNormal();

            float angle = normal0.AngleTo(normal1);
            if (minAngle <= angle && angle <= maxAngle) yield return edge;
        }
    }

    public static IEnumerable<(SimpleQuadMesh.EdgeRef, SimpleQuadMesh.EdgeRef)> GetEdgeRefsWithAngle(this SimpleQuadMesh quadMesh, float minAngle, float maxAngle)
    {
        foreach (var refs in quadMesh.EdgeRefCache.Values)
        {
            if (refs.Count < 2) continue;
            Vector3 normal0 = quadMesh.Quads[refs[0].FaceIndex].ComputeFaceNormal();
            Vector3 normal1 = quadMesh.Quads[refs[1].FaceIndex].ComputeFaceNormal();

            float angle = normal0.AngleTo(normal1);
            if (minAngle <= angle && angle <= maxAngle)
                yield return (refs[0], refs[1]);
        }
    }

    private static (T[], int) GetElements<T>(this IEnumerable<T> enumerable, int amount)
    {
        return enumerable.GetEnumerator().GetElements(amount);
    }

    private static (T[], int) GetElements<T>(this IEnumerator<T> enumerator, int amount)
    {
        T[] array = new T[amount];
        int i = 0;
        while (enumerator.MoveNext() && i < amount)
        {
            array[i] = enumerator.Current;
            i++;
        }
        return (array, i);
    }
}
