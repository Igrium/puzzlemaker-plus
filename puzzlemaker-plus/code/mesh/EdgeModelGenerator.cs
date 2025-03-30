using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PuzzlemakerPlus;

public static class EdgeModelGenerator
{
    public enum CornerType
    {
        None, Corner, MeshEdge
    }

    public static void FindCorners(SimpleQuadMesh mesh, IVoxelView<PuzzlemakerVoxel> world, IDictionary<Vector3I, VoxelCorner> corners)
    {
        foreach (var vertex in mesh.VertexRefCache.Keys)
        {
            Vector3I vertInt = vertex.RoundInt();
            VoxelCorner corner = VoxelCorners.GetCorner(world, vertInt);
            Vector3 normal = corner.GetCornerNormal();

            if (!normal.IsCardinal())
            {
                corners.Add(vertInt, corner);
            }
        }
    }

    public static void IdentifyEdges(SimpleQuadMesh mesh, IVoxelView<PuzzlemakerVoxel> world, Action<Edge> edgeConsumer, bool drawDebug = false)
    {

        Dictionary<Vector3I, VoxelCorner> corners = new();
        FindCorners(mesh, world, corners);

        if (drawDebug)
        {
            foreach (var (vertex, normal) in corners)
            {
                DrawDebugVector(vertex, vertex + normal.GetCornerNormal(), duration: 5);
            }
        }

        Dictionary<Vector3I, HashSet<Vector3>> exaustedTangentsMap = new();

        foreach (var (iVert, corner) in corners)
        {
            HashSet<Vector3> exaustedTangents = exaustedTangentsMap.GetOrAdd(iVert, k => new());
            Vector3 vertex = iVert;
            DirectionFlags visibleEdges = corner.GetVisibleEdges();

            foreach (var direction in visibleEdges.GetDirections())
            {
                Vector3 tangent = direction.GetNormal();
                if (exaustedTangents.Contains(tangent))
                    continue;

                if (drawDebug)
                {
                    DrawDebugVector(vertex, vertex + tangent, Colors.Orange, 4);
                }

                // Find closest vertex in desired direction.
                // Shit complexity, but it's a small sample size at this point.
                Vector3I iClosestOther = default;
                float closestDist = -1;

                foreach (var (iOtherVert, otherCorner) in corners)
                {
                    if (iVert.GetSharedAxis(iOtherVert, out var axis) && axis == direction.GetAxis()) 
                    {
                        // Ensure vertex is in correct direction.
                        if ((iOtherVert[axis] - iVert[axis]) * tangent[axis] < 0)
                            continue;

                        if (!otherCorner.GetVisibleEdges().HasDirection(direction.Opposite()))
                            continue;

                        float dist = vertex.DistanceSquaredTo(iOtherVert);
                        if (closestDist < 0 || dist < closestDist)
                        {
                            closestDist = dist;
                            iClosestOther = iOtherVert;
                        }
                    }
                }

                // No other vertex was found.
                if (closestDist < 0)
                    continue;

                HashSet<Vector3> otherExausted = exaustedTangentsMap.GetOrAdd(iClosestOther, k => new());
                otherExausted.Add(-tangent);

                Edge result = new Edge(iVert, iClosestOther);
                if (drawDebug)
                {
                    QuadMeshExtensions.DebugDrawEdge(result, duration: 4);
                }
                edgeConsumer(result);
            }
        }
    }

    public static void CreateEdgeModel(SimpleQuadMesh mesh, IVoxelView<PuzzlemakerVoxel> world, Action<Quad> quadComsumer, float length = 1, bool drawDebug = false)
    {
        List<Edge> edges = new List<Edge>(mesh.Quads.Length);
        IdentifyEdges(mesh, world, edges.Add, drawDebug);

        foreach (var edge in edges)
        {
            Vector3 normal1 = VoxelCorners.GetCornerNormal(world, edge.Vert1.RoundInt());
            Vector3 normal2 = VoxelCorners.GetCornerNormal(world, edge.Vert2.RoundInt());

            Quad quad = new Quad(edge.Vert1, edge.Vert2, edge.Vert2 + normal2 * length, edge.Vert1 + normal1 * length);
            quad.Normal1 = normal1;
            quad.Normal2 = normal2;
            quad.Normal3 = normal2;
            quad.Normal4 = normal1;

            quadComsumer(quad);
        }
    }

    private static void DrawDebugVector(Vector3 start, Vector3 end, Color? color = null, float duration = 0)
    {
        Callable.From(() => DebugDraw3D.DrawArrow(start, end, color: color, duration: duration)).CallDeferred();
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
}
