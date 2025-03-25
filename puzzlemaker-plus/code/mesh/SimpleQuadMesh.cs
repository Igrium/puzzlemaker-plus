using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;


namespace PuzzlemakerPlus;

/// <summary>
/// A mesh made out of quads that keeps around various caches for quick analysis.
/// </summary>
[GlobalClass]
public partial class SimpleQuadMesh : RefCounted, IEnumerable<Quad>
{
    /// <summary>
    /// A reference to a vertex within a mesh.
    /// </summary>
    public record struct VertexRef
    {
        /// <summary>
        /// The index of the quad the vertex belongs to.
        /// </summary>
        public ushort FaceIndex { get; set; }

        /// <summary>
        /// The index of the vertex within the quad.
        /// </summary>
        public byte VertexIndex { get; set; }

        public VertexRef(ushort faceIndex, byte vertexIndex)
        {
            FaceIndex = faceIndex;
            VertexIndex = vertexIndex;
        }

        public VertexRef(int faceIndex, int vertexIndex)
        {
            FaceIndex = (ushort)faceIndex;
            VertexIndex = (byte)vertexIndex;
        }
    }

    public record struct EdgeRef
    {
        public ushort FaceIndex { get; set; }

        public byte EdgeIndex { get; set; }

        public EdgeRef(ushort faceIndex, byte edgeIndex)
        {
            FaceIndex = faceIndex;
            EdgeIndex = edgeIndex;
        }

        public EdgeRef(int faceIndex, int edgeIndex)
        {
            FaceIndex = (ushort)faceIndex;
            EdgeIndex = (byte)edgeIndex;
        }

        public (byte, byte) GetVertexIndices()
        {
            switch(EdgeIndex)
            {
                case 0: return (0, 1);
                case 1: return (1, 2);
                case 2: return (2, 3);
                case 3: return (3, 0);
                default: throw new IndexOutOfRangeException("invalid edge: " + EdgeIndex);
            }
        }

        public (VertexRef, VertexRef) GetVertices()
        {
            var (vert1, vert2) = GetVertexIndices();
            return (new VertexRef(FaceIndex, vert1), new VertexRef(EdgeIndex, vert2));
        }
    }

    private Quad[] _quads;
    public Quad[] Quads => _quads;

    private Dictionary<Vector3, IReadOnlyList<VertexRef>> _vertexRefCache = new();

    /// <summary>
    /// A mapping of vertex positions to all the face corners that are at that position.
    /// </summary>
    public IReadOnlyDictionary<Vector3, IReadOnlyList<VertexRef>> VertexRefCache => _vertexRefCache;

    private Dictionary<Edge, IReadOnlyList<EdgeRef>> _edgeRefCache = new();

    /// <summary>
    /// A mapping of vertex positions to all the edges that adjoin that vertex.
    /// </summary>
    public IReadOnlyDictionary<Edge, IReadOnlyList<EdgeRef>> EdgeRefCache => _edgeRefCache;

    public SimpleQuadMesh(Quad[] quads)
    {
        _quads = quads;
        AssertNotTooBig();
    }

    public SimpleQuadMesh(ReadOnlySpan<Quad> quads)
    {
        _quads = quads.ToArray();
        AssertNotTooBig();
    }

    public SimpleQuadMesh(SimpleQuadMesh other)
    {
        _quads = new Quad[other.Quads.Length];
        Array.Copy(other.Quads, _quads, _quads.Length);

        // We can shallow copy because the list values are never updated after ComputeVertexCache returns.
        _vertexRefCache = new(other.VertexRefCache);
        _edgeRefCache = new(other.EdgeRefCache);
    }

    public SimpleQuadMesh(IEnumerable<Quad> quads)
    {
        _quads = quads.ToArray();
        AssertNotTooBig();
    }

    private void AssertNotTooBig()
    {
        if (_quads.Length > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException("Maximum allowed quads: " + ushort.MaxValue);
        }
    }

    public void ComputeVertexCache()
    {
        _vertexRefCache.Clear();

        for (ushort faceIndex = 0; faceIndex < _quads.Length; faceIndex++)
        {
            ref Quad quad = ref _quads[faceIndex];
            for (byte vertIndex = 0; vertIndex < 4; vertIndex++)
            {
                Vector3 vert = quad[vertIndex];
                List<VertexRef> list;
                if (_vertexRefCache.TryGetValue(vert, out var l))
                {
                    list = (List<VertexRef>)l;
                }
                else
                {
                    list = new List<VertexRef>();
                    _vertexRefCache.Add(vert, list);
                }

                list.Add(new VertexRef(faceIndex, vertIndex));
            }
        }
    }

    public void ComputeEdgeCache()
    {
        _edgeRefCache.Clear();

        for (ushort faceIndex = 0; faceIndex < _quads.Length; faceIndex++)
        {
            ref Quad quad = ref _quads[faceIndex];
            for (byte edgeIndex = 0; edgeIndex < 4; edgeIndex++)
            {
                Edge edge = quad.GetEdge(edgeIndex);
                List<EdgeRef> list;
                if (_edgeRefCache.TryGetValue(edge, out var l))
                {
                    list = (List<EdgeRef>)l;
                }
                else
                {
                    list = new List<EdgeRef>();
                    _edgeRefCache.Add(edge, list);
                }

                list.Add(new EdgeRef(faceIndex, edgeIndex));
            }
        }
    }

    public Vector3 GetVertex(VertexRef vert)
    {
        return _quads[vert.FaceIndex][vert.VertexIndex];
    }

    public Vector3 GetNormal(VertexRef vert)
    {
        return _quads[vert.FaceIndex].GetNormal(vert.VertexIndex);
    }

    public void SetNormal(VertexRef vert, in Vector3 value)
    {
        _quads[vert.FaceIndex].SetNormal(vert.VertexIndex, value);
    }

    public Vector2 GetUV(VertexRef vert)
    {
        return _quads[vert.FaceIndex].GetUV(vert.VertexIndex);
    }

    public void SetUV(VertexRef vert, Vector2 value)
    {
        _quads[vert.FaceIndex].SetUV(vert.VertexIndex, value);
    }

    public Edge GetEdge(EdgeRef edgeRef)
    {
        return _quads[edgeRef.FaceIndex].GetEdge(edgeRef.EdgeIndex);
    }

    /// <summary>
    /// Iterate through all the vertices in the mesh, and set their normals to the average of all other vertices sharing that position.
    /// Basically "smoothens" the mesh. Requires the vertex cache to have been computed.
    /// </summary>
    /// <remarks>Requires that the vertex cache has been computed.</remarks>
    public void AverageNormals()
    {
        foreach (var refList in _vertexRefCache.Values)
        {
            Vector3 normal = Vector3.Zero;
            int count = 0;

            foreach (var vertRef in refList)
            {
                normal += GetNormal(vertRef);
                count++;
            }

            normal = normal.Normalized();
            foreach (var vertRef in refList)
            {
                SetNormal(vertRef, in normal);
            }
        }
    }

    /// <summary>
    /// Flatten all the normals in the mesh.
    /// </summary>
    public void FlattenNormals()
    {
        for (int i = 0; i < _quads.Length; i++)
        {
            _quads[i].ResetNormals();
        }
    }

    public IEnumerable<ushort> GetAdjoiningFaceIndices(Vector3 vertex)
    {
        if (_vertexRefCache.TryGetValue(vertex, out var refs))
        {
            return refs.Select(r => r.FaceIndex);
        }
        else return Enumerable.Empty<ushort>();
    }

    /// <summary>
    /// Get an enumerable of all the quads that contain this vertex.
    /// </summary>
    /// <param name="vertex">The vertex.</param>
    /// <returns>All quads with the vertex.</returns>
    /// <remarks>Requires that the vertex cache has been computed.</remarks>
    public IEnumerable<Quad> GetAdjoiningFaces(Vector3 vertex)
    {
        return GetAdjoiningFaceIndices(vertex).Select(s => _quads[s]);
    }

    public IEnumerable<ushort> GetAdjoiningFaceIndices(Edge edge)
    {
        if (_edgeRefCache.TryGetValue(edge, out var refs))
        {
            return refs.Select(r => r.FaceIndex);
        }
        else return Enumerable.Empty<ushort>();
    }

    /// <summary>
    /// Get an enumerable of all the quads that contain this edge.
    /// </summary>
    /// <param name="edge">The edge.</param>
    /// <returns>All quads with the edge.</returns>
    /// <remarks>Requires that the edge cache has been computed.</remarks>
    public IEnumerable<Quad> GetAdjoiningFaces(Edge edge)
    {
        return GetAdjoiningFaceIndices(edge).Select(s => _quads[s]);
    }

    public IEnumerable<EdgeRef> GetAdjoiningEdgeRefs(Vector3 vertex)
    {
        foreach (int faceIndex in GetAdjoiningFaceIndices(vertex))
        {
            for (int i = 0; i < 4; i++)
            {
                if (_quads[faceIndex].GetEdge(i).Contains(vertex))
                    yield return new EdgeRef(faceIndex, i);
            }
        }
    }

    /// <summary>
    /// Get an enumerable of all the edges that contain this vertex.
    /// </summary>
    /// <param name="vertex">The vertex.</param>
    /// <returns>All edges with that vertex.</returns>
    /// <remarks>There will likely be duplicate edges if multiple faces share an edge.
    /// Requires that the edge cache has been computed.</remarks>
    public IEnumerable<Edge> GetAdjoiningEdges(Vector3 vertex)
    {
        return GetAdjoiningEdgeRefs(vertex).Select(GetEdge);
    }

    public IEnumerator<Quad> GetEnumerator()
    {
        return ((IEnumerable<Quad>)_quads).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _quads.GetEnumerator();
    }

    /// <summary>
    /// Export this quad mesh into an ArrayMesh.
    /// </summary>
    /// <param name="mesh">Mesh to export into.</param>
    /// <param name="splitMaterials">If set, create multiple surfaces based on quad material index.</param>
    /// <param name="materials">Materials to add, if any.</param>
    public void ToArrayMesh(ArrayMesh mesh, bool splitMaterials = false, params Material[] materials)
    {
        mesh.ClearSurfaces();
        IMeshBuilder builder = splitMaterials ? new MultiMeshBuilder() : new MeshBuilder();
        foreach (var quad in _quads)
        {
            builder.AddQuad(quad);
        }
        builder.ToMesh(mesh, materials);
    }
}
