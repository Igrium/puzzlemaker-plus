﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using VMFLib.Objects;

namespace PuzzlemakerPlus;

internal static class VectorExtensions
{

    public static Vec3 AsVec3(this Vector3 vec)
    {
        return new Vec3(vec.X, vec.Y, vec.Z);
    }

    public static Vector3I RoundInt(this Vector3 vec)
    {
        vec = vec.Round();
        return new Vector3I((int)vec.X, (int)vec.Y, (int)vec.Z);
    }

    public static Vector3I CeilInt(this Vector3 vec)
    {
        vec = vec.Ceil();
        return new Vector3I((int)vec.X, (int)vec.Y, (int)vec.Z);
    }

    public static Vector3I FloorInt(this Vector3 vec)
    {
        vec = vec.Floor();
        return new Vector3I((int)vec.X, (int)vec.Y, (int)vec.Z);
    }

    public static Vector3I Truncate(this Vector3 vec)
    {
        return new Vector3I((int)vec.X, (int)vec.Y, (int)vec.Z);
    }

    /// <summary>
    /// Round each component of a vector to the nearest multiple of a given number.
    /// </summary>
    /// <param name="vec">The vector.</param>
    /// <param name="multipleOf">Number to round to multiple of.</param>
    /// <returns></returns>
    public static Vector3 Round(this Vector3 vec, float multipleOf)
    {
        if (multipleOf <= 0)
            return vec;
        return new Vector3()
        {
            X = MathF.Round(vec.X / multipleOf) * multipleOf,
            Y = MathF.Round(vec.Y / multipleOf) * multipleOf,
            Z = MathF.Round(vec.Z / multipleOf) * multipleOf
        };
    }

    public static int ManhattanDist(this Vector3I vec1, Vector3I vec2)
    {
        return Math.Abs(vec1.X - vec2.X) + Math.Abs(vec1.Y - vec2.Y) + Math.Abs(vec1.Z - vec2.Z);
    }

    public static Aabb Move(this in Aabb aabb, in Vector3 vec) 
    {
        return new Aabb(aabb.Position + vec, aabb.Size);
    }

    /// <summary>
    /// Get the chunk that a given voxel belongs to.
    /// </summary>
    /// <param name="vec">Global voxel position.</param>
    /// <returns>Chunk coordinate.</returns>
    public static Vector3I GetChunk(this in Vector3I vec)
    {
        return new Vector3I(vec.X >> 4, vec.Y >> 4, vec.Z >> 4);
    }

    public static Vector3I ChunkStartPos(this in Vector3I vec)
    {
        return new Vector3I(vec.X << 4, vec.Y << 4, vec.Z << 4);
    }

    /// <summary>
    /// Get the position of a voxel relative to its local chunk.
    /// </summary>
    /// <param name="vec">Global voxel position.</param>
    /// <returns>Chunk-relative position.</returns>
    public static Vector3I GetChunkLocalPos(this in Vector3I vec)
    {
        return new Vector3I(vec.X & 15, vec.Y & 15, vec.Z & 15);
    }
}
