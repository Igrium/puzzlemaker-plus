using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

    /// <summary>
    /// Find the edge of a given vertex that closest matches a given tangent.
    /// </summary>
    /// <param name="quadMesh">Target mesh.</param>
    /// <param name="vertex">The vertex to search.</param>
    /// <param name="tangent">The tangent to compare to.</param>
    /// <returns>The closest edge; <c>Edge.default</c> if the vertex was not in the mesh.</returns>
    public static Edge GetClosestEdge(this SimpleQuadMesh quadMesh, Vector3 vertex, Vector3 tangent)
    {
        bool init = false;
        Edge closestEdge = default;
        float closestDot = 0;
        tangent = tangent.Normalized();

        foreach (var edge in quadMesh.GetAdjoiningEdges(vertex))
        {
            Vector3 edgeTangent = edge.GetTangent(vertex);
            edgeTangent = edgeTangent.Normalized();

            float dot = edgeTangent.Dot(tangent);
            if (!init || dot > closestDot)
            {
                closestEdge = edge;
                closestDot = dot;
                init = true;
            }
        }

        return closestEdge;
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

    /// <summary>
    /// Configures TraverseEdgeLine.
    /// </summary>
    public struct EdgeTraversalConfig
    {
        /// <summary>
        /// The mesh to use.
        /// </summary>
        public SimpleQuadMesh Mesh;

        /// <summary>
        /// Direction to traverse line in.
        /// </summary>
        public Vector3 Tangent;

        /// <summary>
        /// Returns true if we should stop traversing down the line.
        /// </summary>
        public Predicate<Vector3> EndCondition;

        /// <summary>
        /// Called when the end condition returns true for any vertex.
        /// </summary>
        public Action<Vector3> OnConditionMet;

        /// <summary>
        /// The maximum amount we can deviate from the supplied tangent.
        /// </summary>
        public float MaxDeviation;

        public bool DrawDebug;
        public Color? DebugColor;
    }

    /// <summary>
    /// Traverse a straight line of edges, possibly branching if colinear edges are found, until an end condition is met.
    /// </summary>
    /// <param name="config">Method arguments.</param>
    /// <param name="vertex">The vertex to start on.</param>
    /// <remarks>Requires that the edge cache has been generated.</remarks>
    public static void TraverseEdgeLine(ref EdgeTraversalConfig config, Vector3 vertex)
    {
        TraverseEdgeLine(ref config, vertex, new HashSet<Vector3>());
    }

    public static void TraverseEdgeLine(ref EdgeTraversalConfig config, Vector3 vertex, ISet<Vector3> saturatedVerts)
    {
        if (config.MaxDeviation == 0)
            config.MaxDeviation = .05f;

        saturatedVerts.Add(vertex);
        if (config.EndCondition(vertex))
        {
            config.OnConditionMet(vertex);
        }
        else
        {
            foreach (Edge edge in config.Mesh.GetAdjoiningEdges(vertex))
            {
                Vector3 otherVert = vertex == edge.Vert1 ? edge.Vert2 : edge.Vert1;
                Vector3 edgeTan = (otherVert - vertex).Normalized();
                float dot = config.Tangent.Dot(edgeTan);

                if (1 - dot < config.MaxDeviation && !saturatedVerts.Contains(otherVert))
                {
                    if (config.DrawDebug)
                        DebugDrawEdge(edge, config.DebugColor, 2);

                    TraverseEdgeLine(ref config, otherVert, saturatedVerts);
                }
                //else
                //{
                //    if (config.DrawDebug)
                //        DebugDrawEdge(edge, Colors.OrangeRed, .5f);
                //}
            }
        }
    }

    internal static void DebugDrawEdge(Edge edge, Color? color = null, float duration = 0)
    {
        RunOnMainThread(() => DebugDraw3D.DrawLine(edge.Vert1, edge.Vert2, color, duration));
    }

    internal static void DebugDrawPoints(IEnumerable<Vector3> points, float size = .2f, Color? color = null, float duration = 0)
    {
        Vector3[] pointArray = points.ToArray();
        RunOnMainThread(() => DebugDraw3D.DrawPoints(pointArray, size: size, color: color, duration: duration));
    }

    private static void RunOnMainThread(Action action)
    {
        RunOnMainThread(Callable.From(action));
    }

    private static void RunOnMainThread(Callable callable, params Variant[] args)
    {
        if (Thread.CurrentThread.ManagedThreadId == 1)
        {
            callable.Call(args);
        }
        else
        {
            callable.CallDeferred(args);
        }
    }

    //private static void TraverseEdgeLineInternal(this SimpleQuadMesh mesh, Vector3 vertex, Vector3 tangent, Predicate<Vector3> endCondition, Action<Vector3> onConditionMet, HashSet<Vector3> saturatedVerts)
    //{
    //    saturatedVerts.Add(vertex);
    //    if (endCondition(vertex))
    //    {
    //        onConditionMet(vertex);
    //    }
    //    else
    //    {
    //        foreach (Edge edge in mesh.GetAdjoiningEdges(vertex))
    //        {
    //            Vector3 otherVert = vertex == edge.Vert1 ? edge.Vert2 : edge.Vert1;
    //            Vector3 edgeTan = (otherVert - vertex).Normalized();
    //            float dot = tangent.Dot(edgeTan);

    //            if (1 - dot < .05f && !saturatedVerts.Contains(otherVert))
    //            {
    //                TraverseEdgeLineInternal(mesh, otherVert, tangent, endCondition, onConditionMet, saturatedVerts);
    //            }
    //        }
    //    }
    //}
}
