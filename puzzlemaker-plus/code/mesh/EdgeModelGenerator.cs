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

    public static void CreateEdgeModel(SimpleQuadMesh mesh, IVoxelView<PuzzlemakerVoxel> world, Action<Quad> quadConsumer, float length = 1, bool drawDebug = false)
    {
        List<Edge> edges = new List<Edge>(mesh.Quads.Length);
        IdentifyEdges(mesh, world, edges.Add, drawDebug);

        // Shit complexity, but the sample size is like 5
        //foreach (var quad in mesh.Quads)
        //{
        //    for (int i = 0; i < 4; i++)
        //    {
        //        var (index1, index2) = quad.GetEdgeVertIndices(i);
        //        Edge edge = new Edge(quad[index1], quad[index2]);

        //        if (!edges.Any(e => e.IsCollinear(edge)))
        //            continue;

        //        Vector3 faceNormal = -quad.ComputeFaceNormal();

        //        Vector3 normal1 = (faceNormal + quad.GetUniformVertNormal(index1)).Normalized();
        //        Vector3 normal2 = (faceNormal + quad.GetUniformVertNormal(index2)).Normalized();

        //        Quad face = new Quad()
        //        {
        //            Vert1 = edge.Vert1,
        //            Vert2 = edge.Vert2,
        //            Vert3 = edge.Vert2 + normal2 * length,
        //            Vert4 = edge.Vert1 + normal1 * length,

        //            Normal1 = normal1,
        //            Normal2 = normal2,
        //            Normal3 = normal2,
        //            Normal4 = normal1
        //        };

        //        quadConsumer(face);
        //    }
        //}

        foreach (var edge in edges)
        {
            Vector3[] normals1 = VoxelCorners.GetExtrusionNormals(world, edge.Vert1.RoundInt());
            Vector3[] normals2 = VoxelCorners.GetExtrusionNormals(world, edge.Vert2.RoundInt());

            Vector3 normal1 = normals1.FirstOrDefault(Vector3.Zero);
            Vector3 normal2 = normals2.FirstOrDefault(Vector3.Zero);

            Quad quad = new Quad(edge.Vert1, edge.Vert2, edge.Vert2 + normal2 * length, edge.Vert1 + normal1 * length);
            quad.Normal1 = normal1;
            quad.Normal2 = normal2;
            quad.Normal3 = normal2;
            quad.Normal4 = normal1;

            quadConsumer(quad);
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
