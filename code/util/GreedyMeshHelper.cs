using System.Collections.Generic;
using Godot;

namespace PuzzlemakerPlus;

/// <summary>
/// A stateful struct wrapping values involved with testing a single face (or set of faces) in a greedy mesh algorithm.
/// </summary>
public struct GreedyMeshHelper
{
    /// <summary>
    /// Gets called to determine whether a given voxel can be part of this face.
    /// </summary>
    /// <param name="pos">The voxel's world Position.</param>
    /// <returns>If it can be part of this face.</returns>
    public delegate bool CanDrawFace(Vector3I pos);

    private int width = 1;
    private int height = 1;
    private readonly Vector3I facePos;
    private readonly Direction direction;

    public int Width => width;
    public int Height => height;

    /// <summary>
    /// Create a greedy mesh helper.
    /// </summary>
    /// <param name="facePos">Voxel Position of the face to start at. Should be the most-negative face in the plane in global space.</param>
    /// <param name="direction">Which side of the cube the face is on.</param>
    public GreedyMeshHelper(Vector3I facePos, Direction direction)
    {
        this.facePos = facePos;
        this.direction = direction;
    }

    /// <summary>
    /// Get the two opposite corners of the face.
    /// </summary>
    /// <returns>Two opposite corners in no particular order.</returns>
    public (Vector3I, Vector3I) GetCorners()
    {
        return (
            facePos,
            facePos + direction.GetPerpendicularDir1().GetNormal() * width + direction.GetPerpendicularDir2().GetNormal() * height
        );
    }

    /// <summary>
    /// Get a quad as the result of this greedy mesh.
    /// </summary>
    /// <returns>The quad.</returns>
    public Quad GetQuad()
    {
        Quad quad;
        switch (direction)
        {
            case Direction.Up:
                quad = new Quad(
                    new Vector3(0, 1, 0),
                    new Vector3(Width, 1, 0),
                    new Vector3(Width, 1, Height),
                    new Vector3(0, 1, Height));
                break;
            case Direction.Right:
                quad = new Quad(
                    new Vector3(1, Width, Height),
                    new Vector3(1, Width, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(1, 0, Height));
                break;
            case Direction.Back:
                quad = new Quad(
                    new Vector3(0, Height, 1),
                    new Vector3(Width, Height, 1),
                    new Vector3(Width, 0, 1),
                    new Vector3(0, 0, 1));
                break;
            case Direction.Left:
                quad = new Quad(
                    new Vector3(0, Width, 0),
                    new Vector3(0, Width, Height),
                    new Vector3(0, 0, Height),
                    new Vector3(0, 0, 0));
                break;
            case Direction.Forward:
                quad = new Quad(
                    new Vector3(Width, Height, 0),
                    new Vector3(0, Height, 0),
                    new Vector3(0, 0, 0),
                    new Vector3(Width, 0, 0));
                break;
            case Direction.Down:
                quad = new Quad(
                    new Vector3(0, 0, Height),
                    new Vector3(Width, 0, Height),
                    new Vector3(Width, 0, 0),
                    new Vector3(0, 0, 0));
                break;
            default:
                quad = Quad.Empty;
                break;

        }

        quad += facePos;
        quad.ResetNormals();
        quad.FillUVs();
        return quad;
    }

    /// <summary>
    /// Enumerate all the voxels that this face will cover.
    /// </summary>
    /// <returns>All relevent voxel positions.</returns>
    public IEnumerable<Vector3I> GetVoxels()
    {
        var (corner1, corner2) = GetCorners();

        for (int z = corner1.Z; z < corner2.Z; z++)
        {
            for (int y = corner1.Y; y < corner2.Y; y++)
            {
                for (int x = corner1.X; x < corner2.X; x++)
                {
                    yield return new Vector3I(x, y, z);
                }
            }
        }
        yield break;
    }

    public bool CheckRight(CanDrawFace canDrawFaceCallback)
    {
        Vector3I testPos = this.facePos;
        // Move right 1
        testPos += direction.GetPerpendicularDir1().GetNormal() * Width;

        for (int i = 0; i < height; i++)
        {
            if (!canDrawFaceCallback(testPos))
                return false;
            // Move up 1
            testPos += direction.GetPerpendicularDir2().GetNormal();
        }
        return true;
    }

    public bool TryExpandRight(CanDrawFace canDrawFaceCallback)
    {
        if (CheckRight(canDrawFaceCallback))
        {
            width++;
            return true;
        }
        else return false;
    }

    public bool CheckUp(CanDrawFace canDrawFaceCallback)
    {
        Vector3I testPos = this.facePos;
        // Move up 1
        testPos += direction.GetPerpendicularDir2().GetNormal() * Height;

        for (int i = 0; i < width; i++)
        {
            if (!canDrawFaceCallback(testPos))
                return false;

            // Move right 1
            testPos += direction.GetPerpendicularDir1().GetNormal();
        }
        return true;
    }

    public bool TryExpandUp(CanDrawFace canDrawFaceCallback)
    {
        if (CheckUp(canDrawFaceCallback))
        {
            height++;
            return true;
        }
        else return false;
    }

    /// <summary>
    /// Fully expand this face, prioritizing PerpendicularDir1.
    /// </summary>
    /// <returns>The size of this face.</returns>
    public Vector2I ExpandRight(CanDrawFace canDrawFaceCallback)
    {
        while (CheckRight(canDrawFaceCallback))
        {
            width++;
        }
        while (CheckUp(canDrawFaceCallback))
        {
            height++;
        }
        return new Vector2I(width, height);
    }

    /// <summary>
    /// Fully expand this face, prioritizing PerpendicularDir2.
    /// </summary>
    /// <returns>The size of this face.</returns>
    public Vector2I ExpandUp(CanDrawFace canDrawFaceCallback)
    {
        while (CheckUp(canDrawFaceCallback))
        {
            height++;
        }
        while(CheckRight(canDrawFaceCallback))
        {
            width++;
        }
        return new Vector2I(width, height);
    }

    /// <summary>
    /// Attempt to expand this mesh in a square fashon.
    /// </summary>
    /// <returns>The size of this face.</returns>
    public Vector2I ExpandSquare(CanDrawFace canDrawFaceCallback)
    {
        bool canExpandRight = true;
        bool canExpandUp = true;

        do
        {
            if (canExpandRight)
                canExpandRight = TryExpandRight(canDrawFaceCallback);

            if (canExpandUp)
                canExpandUp = TryExpandUp(canDrawFaceCallback);
        }
        while (canExpandRight || canExpandUp);

        return new Vector2I(width, height);
    }
}
