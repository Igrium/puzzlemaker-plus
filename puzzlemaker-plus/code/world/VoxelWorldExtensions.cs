using System.Linq;
using Godot;
namespace PuzzlemakerPlus;

public static class VoxelWorldExtensions
{
    public static bool IsEmpty(this VoxelChunk<PuzzlemakerVoxel> chunk)
    {
        foreach (var val in chunk.Data)
        {
            if (val.IsOpen)
                return false;
        }
        return true;
    }

    public static (Vector3I, Vector3I) GetFilledBounds(this VoxelChunk<PuzzlemakerVoxel> chunk)
    {
        Vector3I max = new Vector3I(-1, -1, -1);
        Vector3I min = new Vector3I(16, 16, 16);

        for (int z = 0; z < 16; z++)
        {
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (chunk.GetVoxel(x, y, z).IsOpen)
                    {
                        if (x < min.X) min.X = x;
                        if (y < min.Y) min.Y = y;
                        if (z < min.Z) min.Z = z;

                        if (x > max.X) max.X = x;
                        if (y > max.Y) max.Y = y;
                        if (z > max.Z) max.Z = z;
                    }
                }
            }
        }

        return (min, max);
    }

    /// <summary>
    /// Check if this world is empty.
    /// </summary>
    /// <returns>True if there are no voxels in the world with the IsOpen flag.</returns>
    public static bool IsEmpty(this VoxelWorld<PuzzlemakerVoxel> world)
    {
        if (!world.Chunks.Any())
            return true;

        foreach (var chunk in world.Chunks.Values)
        {
            if (!chunk.IsEmpty())
                return false;
        }
        return true;
    }

    /// <summary>
    /// Get the bounding box of the filled blocks in the world.
    /// </summary>
    /// <returns>Bounding box, inclusive. If min > max, there were no blocks in the world.</returns>
    /// <remarks>If the world is not pruned, empty chunks can contribute to the bounds.</remarks>
    public static (Vector3I, Vector3I) GetFilledBounds(this VoxelWorld<PuzzlemakerVoxel> world)
    {
        if (!world.Chunks.Any())
            return (Vector3I.Zero, Vector3I.Zero);

        // Identify min/max chunks first to avoid scanning middle chunks.
        Vector3I minChunk = Vector3I.MaxValue;
        Vector3I maxChunk = Vector3I.MinValue;

        foreach (var pos in world.Chunks.Keys)
        {
            if (pos.X < minChunk.X) minChunk.X = pos.X;
            if (pos.Y < minChunk.Y) minChunk.Y = pos.Y;
            if (pos.Z < minChunk.Z) minChunk.Z = pos.Z;

            if (pos.X > maxChunk.X) maxChunk.X = pos.X;
            if (pos.Y > maxChunk.Y) maxChunk.Y = pos.Y;
            if (pos.Z > maxChunk.Z) maxChunk.Z = pos.Z;
        }

        Vector3I min = Vector3I.MaxValue;
        Vector3I max = Vector3I.MinValue;

        foreach (var (pos, chunk) in world.Chunks)
        {
            if (pos.X == minChunk.X || pos.X == maxChunk.X
                || pos.Y == minChunk.Y || pos.Y == maxChunk.Y
                || pos.Z == minChunk.Z || pos.Z == maxChunk.Z)
            {
                var (chunkMin, chunkMax) = chunk.GetFilledBounds();

                chunkMin += pos.ChunkStartPos();
                chunkMax += pos.ChunkStartPos();

                if (chunkMin.X < min.X) min.X = chunkMin.X;
                if (chunkMin.Y < min.Y) min.Y = chunkMin.Y;
                if (chunkMin.Z < min.Z) min.Z = chunkMin.Z;

                if (chunkMax.X > max.X) max.X = chunkMax.X;
                if (chunkMax.Y > max.Y) max.Y = chunkMax.Y;
                if (chunkMax.Z > max.Z) max.Z = chunkMax.Z;
            }
        }

        return (min, max);
    }
}
