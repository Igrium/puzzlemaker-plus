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

    public static void FindCorners(SimpleQuadMesh mesh, IVoxelView<PuzzlemakerVoxel> world, IDictionary<Vector3I, Vector3> corners)
    {
        foreach (var vertex in mesh.VertexRefCache.Keys)
        {
            Vector3I vertInt = vertex.RoundInt();
            Vector3 normal = VoxelCorners.GetCornerNormal(world, vertInt);

            if (!normal.IsCardinal())
            {
                corners.Add(vertInt, normal);
            }
        }
    }

    public static void IdentifyEdges(SimpleQuadMesh mesh, IVoxelView<PuzzlemakerVoxel> world, Action<Edge> edgeConsumer, bool drawDebug = false)
    {

        Dictionary<Vector3I, Vector3> corners = new();
        FindCorners(mesh, world, corners);

        if (drawDebug)
        {
            foreach (var (vertex, normal) in corners)
            {
                DrawDebugVector(vertex, vertex + normal, duration: 5);
            }
        }

        Dictionary<Vector3I, HashSet<Vector3>> exaustedTangentsMap = new();


        foreach (var (iVert, normal) in corners)
        {
            HashSet<Vector3> exaustedTangents = exaustedTangentsMap.GetOrAdd(iVert, k => new());

            Vector3 vertex = iVert;
            DirectionFlags visibleEdges = VoxelCorners.GetVisibleEdges(world, iVert);

            foreach (var edge in mesh.GetAdjoiningEdges(vertex).Distinct())
            {

                //Vector3 otherVert = vertex == edge.Vert1 ? edge.Vert2 : edge.Vert1;
                //Vector3 tangent = (otherVert - vertex).Normalized();
                Vector3 tangent = edge.GetTangent(vertex);
                Direction direction = Directions.GetClosestDirection(tangent);

                if (!visibleEdges.HasDirection(direction))
                    continue;

                if (exaustedTangents.Contains(tangent))
                    continue;

                if (drawDebug)
                {
                    DrawDebugVector(vertex, vertex + tangent, Colors.Orange, 5);
                }

                // Find closest vertex in the desired direction.
                // Shit complexity, but but it's a small(ish) sample at this point.
                Vector3I iClosestOther = default;
                float closestDistance = -1;

                foreach (var iOtherVert in corners.Keys)
                {
                    if (iVert.GetSharedAxis(iOtherVert, out var axis) && axis == direction.GetAxis())
                    {
                        // Ensure the vertex is in the correct direction (tangent is negative if it's supposed to be inverted)
                        if ((iOtherVert[axis] - iVert[axis]) * tangent[axis] < 0)
                            continue;

                        if (!VoxelCorners.GetVisibleEdges(world, iOtherVert).HasDirection(direction.Opposite()))
                            continue;

                        float dist = vertex.DistanceSquaredTo(iOtherVert);
                        if (closestDistance < 0 || dist < closestDistance)
                        {
                            iClosestOther = iOtherVert;
                            closestDistance = dist;
                        }
                    }
                }

                if (closestDistance < 0)
                    continue; // No other vertices were found on this axis.

                HashSet<Vector3> otherExausted = exaustedTangentsMap.GetOrAdd(iClosestOther, k => new());
                otherExausted.Add(-tangent);

                Edge result = new Edge(vertex, iClosestOther);
                if (drawDebug)
                {
                    QuadMeshExtensions.DebugDrawEdge(edge, duration: 4);
                }
                edgeConsumer(result);

                //Color color = Color.FromHsv(Random.Shared.NextSingle(), 1, 1);

                //QuadMeshExtensions.EdgeTraversalConfig config = new()
                //{
                //    Mesh = mesh,
                //    Tangent = tangent,
                //    EndCondition = vec => corners.ContainsKey(vec.RoundInt()),
                //    OnConditionMet = cornerCache.Add,
                //};

                //if (drawDebug)
                //{
                //    config.DrawDebug = true;
                //    config.DebugColor = color;
                //}

                //QuadMeshExtensions.TraverseEdgeLine(ref config, vertex);

                //if (cornerCache.Count == 0)
                //    continue;

                //// Get closest value
                //Vector3 lineEnd = default;
                //float minDist = float.MaxValue;
                //foreach (Vector3 vec in cornerCache)
                //{
                //    float dist = vec.DistanceSquaredTo(vertex);
                //    if (dist < minDist)
                //    {
                //        lineEnd = vec;
                //        minDist = dist;
                //    }
                //}

                //Edge result = new Edge(vertex, lineEnd);
                //if (drawDebug)
                //{
                //    QuadMeshExtensions.DebugDrawEdge(edge, duration: 5);
                //}
                //edgeConsumer(result);
            }
        }
    }

    public static void CreateEdgeModel(SimpleQuadMesh mesh, IVoxelView<PuzzlemakerVoxel> world, Action<Quad> quadComsumer, float length = 1, bool drawDebug = false)
    {
        List<Edge> edges = new List<Edge>(mesh.Quads.Length);

        IdentifyEdges(mesh, world, edges.Add, drawDebug);
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
