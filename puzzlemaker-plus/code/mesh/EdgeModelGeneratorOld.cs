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
public static class EdgeModelGeneratorOld
{
    public enum CornerType
    {
        None, Corner, MeshEdge
    }

    /// <summary>
    /// Identify every edge in the mesh that's visually connected to two corners.
    /// </summary>
    /// <param name="mesh">The mesh in question</param>
    /// <param name="edgeConsumer">Called for every edge.</param>
    public static void IdentifyEdges(SimpleQuadMesh mesh, Action<Edge, CornerType, CornerType> edgeConsumer, bool drawDebug = false)
    {

        CornerType getCornerType(Vector3 vertex)
        {
            ushort[] indices = mesh.GetAdjoiningFaceIndices(vertex).ToArray();
            if (indices.Length == 3)
            {
                Quad[] quads = mesh.Quads;

                Vector3 normal1 = quads[indices[0]].ComputeFaceNormal();
                Vector3 normal2 = quads[indices[1]].ComputeFaceNormal();
                Vector3 normal3 = quads[indices[2]].ComputeFaceNormal();

                float maxAngle = Mathf.Pi * .75f;

                if (normal1.AngleTo(-normal2) < maxAngle &&
                    normal1.AngleTo(-normal3) < maxAngle &&
                    normal2.AngleTo(-normal3) < maxAngle)
                    return CornerType.Corner;
            }
            else if (indices.Length == 2)
            {
                Vector3 normal1 = mesh.Quads[indices[0]].ComputeFaceNormal();
                Vector3 normal2 = mesh.Quads[indices[1]].ComputeFaceNormal();
                if (normal1.AngleTo(-normal2) < Mathf.Pi * .75f)
                    return CornerType.MeshEdge;
            }
            return CornerType.None;
        }

        Dictionary<Vector3, HashSet<Vector3>> exaustedTangentsMap = new();

        LinkedList<Vector3> cornerCache = new();

        foreach (var vertex in mesh.VertexRefCache.Keys)
        {
            CornerType thisCornerType = getCornerType(vertex);
            if (thisCornerType != CornerType.Corner) continue;

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
                    EndCondition = vert => getCornerType(vert) != CornerType.None,
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
                edgeConsumer(new Edge(vertex, lineEnd), thisCornerType, getCornerType(lineEnd));
            }
        }
    }

    // Very crude implementation doesn't check if the shared verts are actually an edge.
    private static bool SharesAnEdge(in Quad quad, in Quad other)
    {
        int shared = 0;
        for (int i = 0; i < 4; i++)
        {
            Vector3 vert = quad[i];
            for (int x = 0; x < 4; x++)
            {
                if (other[x] == vert)
                {
                    shared++;
                    break;
                }
            }
            if (shared >= 2)
                return true;
        }
        return false;
    }

    public static void CreateEdgeModel(SimpleQuadMesh mesh, IVoxelView<PuzzlemakerVoxel> world, Action<Quad> quadComsumer, float length = 1)
    {
        List<(Edge, CornerType, CornerType)> edges = new(mesh.Quads.Length);
        IdentifyEdges(mesh, (edge, corner1, corner2) => edges.Add((edge, corner1, corner2)));

        foreach (var (edge, corner1, corner2) in edges)
        {
            //SimpleQuadMesh.VertexRef vert1Ref = mesh.VertexRefCache[edge.Vert1][0];
            //SimpleQuadMesh.VertexRef vert2Ref = mesh.VertexRefCache[edge.Vert2][0];

            Vector3 normal1 = corner1 == CornerType.Corner ? GetCornerNormal(mesh, edge.Vert1) : GetCornerNormal(mesh, world, edge.Vert1);
            Vector3 normal2 = corner2 == CornerType.Corner ? GetCornerNormal(mesh, edge.Vert2) : GetCornerNormal (mesh, world, edge.Vert2);

            //Vector3 normal1 = -mesh.GetNormal(vert1Ref);
            //Vector3 normal2 = -mesh.GetNormal(vert2Ref);

            Quad quad = new Quad(edge.Vert1, edge.Vert2, edge.Vert2 + normal2 * length, edge.Vert1 + normal1 * length);
            quad.Normal1 = normal1;
            quad.Normal2 = normal2;
            quad.Normal3 = normal2;
            quad.Normal4 = normal1;

            quadComsumer(quad);
        }
    }

    // Get a corner normal based on the average of its normals in the mesh.
    private static Vector3 GetCornerNormal(SimpleQuadMesh mesh, Vector3 vertex)
    {
        var cache = mesh.VertexRefCache[vertex];
        Vector3 normal = Vector3.Zero;
        foreach (var r in cache)
        {
            normal += mesh.GetNormal(r);
        }
        return -normal.Normalized();
    }

    // Compute a corner normal based on the blocks in the world.
    // Required for vertices where the normal may be impacted for blocks outside the chunk.
    private static Vector3 GetCornerNormal(SimpleQuadMesh mesh, IVoxelView<PuzzlemakerVoxel> world, Vector3 vertex)
    {
        return GetCornerNormal(mesh, vertex); // TODO: actually implement this
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
