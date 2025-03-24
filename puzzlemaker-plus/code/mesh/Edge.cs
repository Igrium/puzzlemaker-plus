using Godot;
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