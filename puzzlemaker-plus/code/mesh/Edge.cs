using Godot;
using System;
using System.Diagnostics.CodeAnalysis;


namespace PuzzlemakerPlus;
/// <summary>
/// Represents two, unordered points of an edge.
/// </summary>
public struct Edge
{
    public Vector3 Vert1 { get; set; }
    public Vector3 Vert2 { get; set; }

    public Edge(Vector3 pos1, Vector3 pos2)
    {
        Vert1 = pos1;
        Vert2 = pos2;
    }

    public override string ToString()
    {
        return $"Edge[Pos1: {Vert1}, Pos2: {Vert2}]";
    }

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Edge && this.Equals((Edge)obj);

    public bool Contains(Vector3 vertex)
    {
        return vertex == Vert1 || vertex == Vert2;
    }

    /// <summary>
    /// Get the edge's tangent relative to one of its vertices.
    /// </summary>
    /// <param name="vertex">The vertex in question.</param>
    /// <returns>The tangent.</returns>
    /// <exception cref="ArgumentException">If the supplied vertex is not in this edge.</exception>
    public Vector3 GetTangent(Vector3 vertex)
    {
        if (vertex == Vert1)
        {
            return (Vert2 - Vert1).Normalized();
        }
        else if (vertex == Vert2)
        {
            return (Vert1 - Vert2).Normalized();
        }
        else
        {
            throw new ArgumentException("Vertex is not on this edge.", nameof(vertex));
        }
    }

    public bool Equals(Edge other)
    {
        return (this.Vert1 == other.Vert1 && this.Vert2 == other.Vert2) || (this.Vert1 == other.Vert2 && this.Vert2 == other.Vert1);
    }

    public override int GetHashCode()
    {
        return Vert1.GetHashCode() + Vert2.GetHashCode(); // Commutitive property
    }

    public static bool operator ==(Edge left, Edge right) => left.Equals(right);

    public static bool operator !=(Edge left, Edge right) => !left.Equals(right);
}