using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// Generates the extruded edges seen in corners.
/// </summary>
public static class EdgeModelGenerator
{
    /// <summary>
    /// Called every time a quad is generated from an edge.
    /// </summary>
    /// <param name="quad">The generated quad. The quad's normals will match the normal of the vertex it was generated from.</param>
    /// <remarks>Vert1 and Vert2 of the quad will always make up the edge it was generated from.</remarks>
    public delegate void EdgeQuadConsumer(Quad quad);

    /// <summary>
    /// Generate an edge model from a quad mesh.
    /// </summary>
    /// <param name="quadMesh">Quad mesh to generate from.</param>
    /// <param name="quadConsumer">Called every time a quad is generated.</param>
    /// <param name="minAngle">Minimum edge angle in radians.</param>
    /// <param name="MaxAngle">Maximum edge angle in radians.</param>
    /// <remarks>Edge extensions are based on the vertex normals of the first quad found. Call AverageNormals() first for proper results.
    /// Direction of the generated face is undefined; it should be rendered dual-sidedly.</remarks>
    public static void GenerateEdgeModel(
        SimpleQuadMesh quadMesh,
        EdgeQuadConsumer quadConsumer,
        float minAngle = Mathf.Pi / 4, // 45 degrees
        float MaxAngle = Mathf.Pi / 1.5f) // 120 degrees
    {
        quadMesh = new SimpleQuadMesh(quadMesh);
        foreach (var (edge, _) in quadMesh.GetEdgeRefsWithAngle(minAngle, MaxAngle))
        {
            // Only need to use the first ref

            var (vertRef1, vertRef2) = edge.GetVertices();

            Vector3 vert1 = quadMesh.GetVertex(vertRef1);
            Vector3 vert2 = quadMesh.GetVertex(vertRef2);

            Vector3 normal1 = -quadMesh.GetNormal(vertRef1);
            Vector3 normal2 = -quadMesh.GetNormal(vertRef2);

            Quad quad = new Quad(vert1, vert2, vert2 + normal2, vert1 + normal1);

            quad.Normal1 = normal1;
            quad.Normal2 = normal2;
            quad.Normal3 = normal2;
            quad.Normal4 = normal1;

            quadConsumer(quad);
        }
    }

    public static void GenerateEdgeModel(SimpleQuadMesh quadMesh, Action<Quad> quadConsumer)
    {
        // Corners are identified by vertices that have exactly 3 quads attached to them.
        // Because in this context quads will always be rectangular, the only way for 3 to adjoin one face
        // barring invalid geometry is to be a corner.
        bool isCorner(Vector3 vertex) => quadMesh.GetAdjoiningFaceIndices(vertex).Count() == 3;

        HashSet<Vector3> processedCorners = new();

        // A map of all vertices and directions where the mesh is already generated.
        Dictionary<Vector3, HashSet<Vector3>> exaustedTangentsMap = new(); 

        foreach (var (vertex, vertrefs) in quadMesh.VertexRefCache)
        {
            // Each vertex ref will be a separate face.
            if (vertrefs.Count != 3) continue;

            HashSet<Edge> edges = new(quadMesh.GetAdjoiningEdges(vertex));
            if (edges.Count != 3)
                GD.PushWarning($"A vertex was found with 3 faces but {edges.Count} edges. How did this happen??");

            HashSet<Vector3> exaustedTangents = exaustedTangentsMap.GetOrAdd(vertex, k => new());
            foreach (var edge in edges)
            {
                // Technically this will lead to skipping edges, but we assume that edges sharing tangents will end up in the same vertex eventually.
                Vector3 tangent = edge.GetTangent(vertex);
                if (exaustedTangents.Contains(tangent))
                    continue;

                // Traverse down edge until we find either another corner or a vertex with only 2 edges.
                if (quadMesh.TraverseEdges(vertex, tangent, isCorner, out var other, out var endTangent))
                {
                    Vector3 thisNormal = quadMesh.GetNormal(vertrefs[0]);
                    // Will be in bounds as long as the cache is valid.
                    Vector3 otherNormal = quadMesh.GetNormal(quadMesh.VertexRefCache[other][0]);

                    Quad quad = new Quad(vertex, other, other + otherNormal, vertex + thisNormal);
                    quadConsumer(quad);

                    exaustedTangentsMap.GetOrAdd(other, k => new()).Add(endTangent);
                }
            }
        }
    }

    /// <summary>
    /// Attempt to traverse a set of edges in a straight line until a predicate is satisfied.
    /// </summary>
    /// <param name="mesh">The mesh.</param>
    /// <param name="startVertex">Vertex to start in.</param>
    /// <param name="tangent">Direction to traverse in.</param>
    /// <param name="shouldStop">Predicate to test if we should stop traversing.</param>
    /// <param name="vertex">The final vertex we land on.</param>
    /// <param name="endTangent">The tangent at from we approached the final vertex.</param>
    /// <returns>True if the predicate was satisfied. False if we hit the end of the mesh first.</returns>
    private static bool TraverseEdges(this SimpleQuadMesh mesh,
        Vector3 startVertex, Vector3 tangent,
        Predicate<Vector3> shouldStop, out Vector3 vertex, out Vector3 endTangent)
    {
        vertex = startVertex;
        bool stop = shouldStop(vertex);
        endTangent = -tangent;
        while (!stop)
        {
            var adjoining = mesh.GetAdjoiningEdges(vertex);
            int edgeCount = GetEdgeWithClosestTangent(adjoining, vertex, tangent, out var edge);

            // Get opposite vertex of edge.
            vertex = edge.Vert1 == vertex ? edge.Vert2 : edge.Vert1;
            endTangent = edge.GetTangent(vertex);
            stop = shouldStop(vertex);

            if (!stop && (edgeCount < 2 || vertex == startVertex))
                return false;
        }
        return true;
    }

    // Return edge count in this function so we only have to enumerate once.
    private static int GetEdgeWithClosestTangent(IEnumerable<Edge> edges, Vector3 vertex, Vector3 tangent, out Edge edge)
    {
        tangent = tangent.Normalized();
        int count = 0;
        float closestDot = 0;
        edge = default;

        foreach (var e in edges)
        {
            Vector3 tan = e.GetTangent(vertex);
            float dot = tan.Dot(tangent);
            if (count == 0 || dot > closestDot)
            {
                edge = e;
                closestDot = dot;
                count++;
            }
        }
        return count;
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
