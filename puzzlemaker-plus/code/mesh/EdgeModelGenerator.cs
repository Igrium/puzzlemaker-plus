using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// Generates the extruded edges seen in corners.
/// </summary>
public static class EdgeModelGenerator
{
    /// <summary>
    /// Identify every edge in the mesh that's visually connected to two corners.
    /// </summary>
    /// <param name="mesh">The mesh in question</param>
    /// <param name="edgeConsumer">Called for every edge.</param>
    public static void IdentifyEdges(SimpleQuadMesh mesh, Action<Edge> edgeConsumer, bool drawDebug = false)
    {

        bool isCorner(Vector3 vertex)
        {
            ushort[] faceIndices = mesh.GetAdjoiningFaceIndices(vertex).ToArray();
            if (faceIndices.Length == 3)
            {
                return true;
            }
            else if (faceIndices.Length == 2)
            {
                Vector3 normal1 = mesh.Quads[faceIndices[0]].ComputeFaceNormal();
                Vector3 normal2 = mesh.Quads[faceIndices[1]].ComputeFaceNormal();
                return normal1.Dot(normal2) < .5f;
            }
            else return false;
        }

        Dictionary<Vector3, HashSet<Vector3>> exaustedTangentsMap = new();

        LinkedList<Vector3> cornerCache = new();

        foreach (var vertex in mesh.VertexRefCache.Keys)
        {
            if (!isCorner(vertex)) continue;

            HashSet<Vector3> exaustedTangents = exaustedTangentsMap.GetOrAdd(vertex, k => new());

            foreach (var edge in mesh.GetAdjoiningEdges(vertex).Distinct())
            {
                Vector3 otherVert = vertex == edge.Vert1 ? edge.Vert2 : edge.Vert1;
                Vector3 tangent = (otherVert - vertex).Normalized();

                if (exaustedTangents.Contains(tangent))
                    continue;

                cornerCache.Clear();
                QuadMeshExtensions.EdgeTraversalConfig config = new()
                {
                    Mesh = mesh,
                    Tangent = tangent,
                    EndCondition = isCorner,
                    OnConditionMet = v => cornerCache.AddLast(v)
                };

                if (drawDebug)
                {
                    config.DrawDebug = true;
                    config.DebugColor = Color.FromHsv(Random.Shared.NextSingle(), 1, 1);
                    QuadMeshExtensions.DebugDrawEdge(edge, config.DebugColor, duration: 2);
                }

                QuadMeshExtensions.TraverseEdgeLine(ref config, otherVert);

                if (cornerCache.Count == 0)
                    continue;

                Vector3 lineEnd = cornerCache.GetLowestValue(vec => vec.DistanceSquaredTo(vertex));

                if (drawDebug)
                {
                    QuadMeshExtensions.DebugDrawPoints(cornerCache, .4f, config.DebugColor, duration: 2);
                }
                

                // Make sure we don't draw the line again when we eventually iterate to the other corner.
                exaustedTangentsMap.GetOrAdd(lineEnd, k => new()).Add(-tangent);
                edgeConsumer(new Edge(vertex, lineEnd));
            }
        }
    }

    private static V GetOrAdd<K, V>(this IDictionary<K, V> dict, K key, Func<K, V> valueFactory)
    {
        if (dict is ConcurrentDictionary<K, V> concurrent)
        {
            return concurrent.GetOrAdd(key, valueFactory);
        }

        if (dict.TryGetValue(key, out var value))
        {
            return value;
        }
        else
        {
            value = valueFactory(key);
            dict.Add(key, value);
            return value;
        }
    }

    private static T? GetLowestValue<T>(this IEnumerable<T> enumerable, Func<T, float> sorter)
    {
        int numValues = 0;
        float lowestScale = 0;
        T? lowest = default;

        foreach (T value in enumerable)
        {
            float scale = sorter(value);
            if (numValues == 0 || scale < lowestScale)
            {
                lowestScale = scale;
                lowest = value;
            }
            numValues++;
        }

        if (numValues == 0)
            GD.PushWarning("enumeration had no values");

        return lowest;
    }

}
