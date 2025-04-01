using System;
using System.Runtime.CompilerServices;
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// A cavity that a voxel edge may have.
/// </summary>
/// <remarks>Cavity is considered from the INSIDE of the voxel.</remarks>
public enum EdgeType
{
    Straight, Convex, Concave
}
public static class EdgeTypes
{
    public static EdgeType GetInverse(this EdgeType edgeType)
    {
        switch (edgeType)
        {
            case EdgeType.Convex: return EdgeType.Concave;
            case EdgeType.Concave: return EdgeType.Convex;
            default: return edgeType;
        }
    }

    public static int GetAngleDegrees(this EdgeType edgeType)
    {
        switch (edgeType)
        {
            case EdgeType.Convex: return 270;
            case EdgeType.Straight: return 180;
            case EdgeType.Concave: return 90;
            default: return 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetAngleRadians(this EdgeType edgeType)
    {
        return Mathf.DegToRad(GetAngleDegrees(edgeType));
    }

    /// <summary>
    /// Determine the normal of an extruded edge based on this contour.
    /// </summary>
    /// <param name="edgeType">Edge contour to use.</param>
    /// <param name="faceNormal">Normal direction of the face in question. Must be rotated!</param>
    /// <param name="edgeTangent">Tangent of the edge. Direction matters.</param> // TODO: verify which direction is which
    /// <returns>The edge normal.</returns>
    public static Vector3 GetEdgeNormal(this EdgeType edgeType, Vector3 faceNormal, Vector3 edgeTangent)
    {
        float offsetDegrees;
        switch (edgeType)
        {
            case EdgeType.Convex:
                offsetDegrees = -45;
                break;
            case EdgeType.Concave:
                offsetDegrees = 45;
                break;
            default:
                offsetDegrees = 0;
                break;
        }

        return faceNormal.Rotated(edgeTangent, Mathf.DegToRad(offsetDegrees));
    }

    /// <summary>
    /// Get the edge type of a voxel within a voxel world.
    /// </summary>
    /// <param name="voxelPredicate">A predicate to test if a given voxel is open, where 0,0,00 is the vertex position.</param>
    /// <param name="face1">One face of the voxel. Should be adjacent to <c>face2</c></param>
    /// <param name="face2">The other face of the voxel. Should be adjacent to <c>face1</c></param>
    /// <returns>The edge type; <c>null</c> if the constructed edge does not have any geometry</returns>
    public static EdgeType? GetEdgeType(Predicate<Vector3I> voxelPredicate, Direction face1, Direction face2)
    {
        if (face1 == face2)
            return null;

        bool isOpen = voxelPredicate(Vector3I.Zero);

        bool dir1Open = voxelPredicate(face1.GetNormal());
        bool dir2Open = voxelPredicate(face2.GetNormal());


        if (dir1Open != dir2Open)
        {
            return EdgeType.Straight;
        }
        // Here on, dir1Open == dir2Open
        else if (dir1Open != isOpen)
        {
            return EdgeType.Concave;
        }
        else if (voxelPredicate(face1.GetNormal() + face2.GetNormal()))
        {
            return EdgeType.Convex;
        }
        else return null;
    }

    public static EdgeType? GetEdgeType(IVoxelView<PuzzlemakerVoxel> world, Vector3I pos, Direction face1, Direction face2)
    {
        return GetEdgeType(vec => world.GetVoxel(vec + pos).IsOpen, face1, face2);
    }
}
