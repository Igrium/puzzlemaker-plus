using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;


namespace PuzzlemakerPlus;

/// <summary>
/// A mesh made out of quads that keeps around various caches for quick analysis
/// </summary>
public class SimpleQuadMesh : IEnumerable<Quad>
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

    private Quad[] _quads;
    public Quad[] Quads => _quads;

    private Dictionary<Vector3, IReadOnlyList<VertexRef>> _vertexRefCache = new();
    public IReadOnlyDictionary<Vector3, IReadOnlyList<VertexRef>> VertexRefCache => _vertexRefCache;


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

    /// <summary>
    /// Iterate through all the vertices in the mesh, and set their normals to the average of all other vertices sharing that position.
    /// Basically "smoothens" the mesh. Requires the vertex cache to have been computed.
    /// </summary>
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

    public IEnumerator<Quad> GetEnumerator()
    {
        return ((IEnumerable<Quad>)_quads).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _quads.GetEnumerator();
    }
}
