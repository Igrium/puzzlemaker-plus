using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus;

[GlobalClass]
[JsonConverter(typeof(PuzzlemakerWorldJsonConverter))]
public partial class PuzzlemakerWorld : VoxelWorld<PuzzlemakerVoxel>
{   

    /// <summary>
    /// Check if this world is empty.
    /// </summary>
    /// <returns>True if there are no voxels in the world with the IsOpen flag.</returns>
    public bool IsEmpty()
    {
        if (!Chunks.Any()) return true;

        foreach (var chunk in Chunks.Values)
        {
            int len = chunk.Data.Length;
            for (int i = 0; i < len; i++)
            {
                if (chunk.Data[i].IsOpen)
                    return false;
            }
        }
        return true;
    }

    public void PruneEmptyChunks()
    {
        var toRemove = Chunks.Where(pair => IsChunkEmpty(pair.Value))
            .Select(pair => pair.Key).ToList();

        foreach (var key in toRemove)
        {
            Chunks.Remove(key);
        }
    }

    /// <summary>
    /// Get the bounding box of the filled blocks in the world.
    /// </summary>
    /// <returns>Bounding box, inclusive. If min > max, there were no blocks in the world.</returns>
    /// <remarks>If the world is not pruned, empty chunks can contribute to the bounds.</remarks>
    public (Vector3I, Vector3I) GetWorldBounds()
    {
        if (!Chunks.Any())
            return (Vector3I.Zero, Vector3I.Zero);

        // Identify min/max chunks first to avoid scanning middle chunks.
        Vector3I minChunk = Vector3I.MaxValue;
        Vector3I maxChunk = Vector3I.MinValue;

        foreach (var pos in Chunks.Keys)
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

        foreach (var (pos, chunk) in Chunks)
        {
            if (pos.X == minChunk.X || pos.X == maxChunk.X
                || pos.Y == minChunk.Y || pos.Y == maxChunk.Y
                || pos.Z == minChunk.Z || pos.Z == maxChunk.Z)
            {
                var (chunkMin, chunkMax) = GetChunkBounds(chunk);

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
    
    /// <summary>
    /// Get the bounds of the filled portion of a pos.
    /// </summary>
    /// <param name="chunk">The pos in question.</param>
    /// <returns>The pos bounds, inclusive. If no block was found, max will be -1, and min will be 16.</returns>
    public static (Vector3I, Vector3I) GetChunkBounds(VoxelChunk<PuzzlemakerVoxel> chunk)
    {
        Vector3I max = new Vector3I(-1, -1, -1);
        Vector3I min = new Vector3I(16, 16, 16);

        for (int z = 0; z < 16; z++)
        {
            for (int y = 0; y <  16; y++)
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

    public bool IsChunkEmpty(VoxelChunk<PuzzlemakerVoxel> chunk)
    {
        foreach (var value in chunk.Data)
        {
            if (value.IsOpen) return false;
        }
        return true;
    }

    /// <summary>
    /// Read a voxel pos from a base-64-encoded string.
    /// </summary>
    /// <param name="base64">Base-64 string.</param>
    /// <returns>The pos.</returns>
    public static VoxelChunk<PuzzlemakerVoxel> ReadChunk(string base64)
    {
        byte[] data = Convert.FromBase64String(base64);
        VoxelChunk<PuzzlemakerVoxel> chunk = new();
        for (int i = 0; i < chunk.Data.Length; i++)
        {
            PuzzlemakerVoxel voxel = new PuzzlemakerVoxel();
            voxel.Flags = data[i];
            chunk.Data[i] = voxel;
        }
        return chunk;
    }

    /// <summary>
    /// Encode a voxel pos into a base-64-encoded string.
    /// </summary>
    /// <param name="chunk">The pos.</param>
    /// <returns>Base-64-encoded string.</returns>
    public static string WriteChunk(VoxelChunk<PuzzlemakerVoxel> chunk)
    {
        byte[] data = new byte[chunk.Data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = chunk.Data[i].Flags;
        }
        return Convert.ToBase64String(data);
    }

}

public sealed class PuzzlemakerWorldJsonConverter : JsonConverter<PuzzlemakerWorld>
{
    public override PuzzlemakerWorld Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Voxel world must be a json object.");


        PuzzlemakerWorld world = new PuzzlemakerWorld();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.Comment)
                continue;
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                //GD.Print($"Loaded {world.Chunks.Count} pos(s) from json.");
                return world;
            }

            // KEY
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            string propertyName = reader.GetString() ?? throw new JsonException();


            // Value
            reader.Read();
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("Chunk data must be a Base-64-encoded string.");

            string value = reader.GetString() ?? throw new JsonException();

            string[] coords = propertyName.Split(',');
            Vector3I pos = new Vector3I(
                int.Parse(coords[0]),
                int.Parse(coords[1]),
                int.Parse(coords[2]));

            VoxelChunk<PuzzlemakerVoxel> chunk = PuzzlemakerWorld.ReadChunk(value);
            world.Chunks.Add(pos, chunk);
        }
        throw new JsonException("Unclosed object in puzzlemaker world");
    }

    public override void Write(Utf8JsonWriter writer, PuzzlemakerWorld value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var (pos, chunk) in value.Chunks)
        {
            writer.WriteString($"{pos.X},{pos.Y},{pos.Z}", PuzzlemakerWorld.WriteChunk(chunk));
        }
        
        writer.WriteEndObject();
    }
}

/// <summary>
/// A single voxel in the puzzlemaker voxel world.
/// While this class has a lot of helper functions, the struct itself is only two bytes.
/// </summary>
public struct PuzzlemakerVoxel
{
    // TODO: We need to find a way to store the subdivision on a per-face basis.
    [Flags]
    public enum VoxelFlags : byte
    {
        Open = 1,
        Up = 2,
        Down = 4,
        Left = 8,
        Right = 16,
        Front = 32,
        Back = 64
    }

    /// <summary>
    /// The bit flags of this voxel. Contains _data about whether it's open and its portalability.
    /// </summary>
    public byte Flags;

    /// <summary>
    /// The subdivision level of the voxel. 0 = 128 hammer units; 1 = 64 hammer units; 2 = 32 hammer units.
    /// </summary>
    public byte Subdivision;

    public bool IsOpen 
    {
        readonly get => HasFlag(VoxelFlags.Open);
        set => SetFlag(VoxelFlags.Open, value);
    }
    public bool UpPortalable
    {
        readonly get => HasFlag(VoxelFlags.Up);
        set => SetFlag(VoxelFlags.Up, value);
    }
    public bool DownPortalable
    {
        readonly get => HasFlag(VoxelFlags.Down);
        set => SetFlag(VoxelFlags.Down, value);
    }
    public bool LeftPortalable
    {
        readonly get => HasFlag(VoxelFlags.Left);
        set => SetFlag(VoxelFlags.Left, value);
    }
    public bool RightPortalable
    {
        readonly get => HasFlag(VoxelFlags.Right);
        set => SetFlag(VoxelFlags.Right, value);
    }
    public bool FrontPortalable
    {
        readonly get => HasFlag(VoxelFlags.Front);
        set => SetFlag(VoxelFlags.Front, value);
    }
    public bool BackPortalable
    {
        readonly get => HasFlag(VoxelFlags.Back);
        set => SetFlag(VoxelFlags.Back, value);
    }

    public readonly PuzzlemakerVoxel WithOpen(bool open)
    {
        PuzzlemakerVoxel result = this;
        result.IsOpen = open;
        return result;
    }

    public readonly bool HasFlag(VoxelFlags flag)
    {
        return ((Flags & (byte)flag) == (byte)flag);
    }


    public void SetFlag(VoxelFlags flag)
    {
        this.Flags |= (byte)flag;
    }

    public void SetFlag(VoxelFlags flag, bool value)
    {
        if (value)
            SetFlag(flag);
        else
            ClearFlag(flag);
    }

    public readonly PuzzlemakerVoxel WithFlag(VoxelFlags flag)
    {
        PuzzlemakerVoxel result = this;
        result.SetFlag(flag);
        return result;
    }

    public void SetFlags(params VoxelFlags[] flags)
    {
        foreach (VoxelFlags flag in flags)
        {
            Flags |= (byte)flag;
        }
    }

    public readonly PuzzlemakerVoxel WithFlags(params VoxelFlags[] flags)
    {
        PuzzlemakerVoxel result = this;
        result.SetFlags(flags);
        return result;
    }

    public void ClearFlag(VoxelFlags flag)
    {
        Flags &= (byte)~flag;
    }

    public readonly PuzzlemakerVoxel WithoutFlag(VoxelFlags flag)
    {
        PuzzlemakerVoxel result = this;
        result.ClearFlag(flag);
        return result;
    }

    public void ClearFlags(params VoxelFlags[] flags)
    {
        foreach (VoxelFlags flag in flags)
        {
            Flags &= (byte)~flag;
        }
    }

    public readonly PuzzlemakerVoxel WithoutFlags(params VoxelFlags[] flags)
    {
        PuzzlemakerVoxel result = this;
        result.ClearFlags(flags);
        return result;
    }

    public PuzzlemakerVoxel WithSubdivision(byte subdivision)
    {
        PuzzlemakerVoxel result = this;
        result.Subdivision = subdivision;
        return result;
    }

    public readonly bool IsPortalable(Direction direction)
    {
        switch(direction)
        {
            case Direction.Up:
                return UpPortalable;
            case Direction.Down:
                return DownPortalable;
            case Direction.Left:
                return LeftPortalable;
            case Direction.Right: 
                return RightPortalable;
            case Direction.Forward:
                return FrontPortalable;
            case Direction.Back:
                return BackPortalable;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction));
        }
    }

    public void SetPortalability(Direction direction, bool value)
    {
        switch(direction)
        {
            case Direction.Up:
                UpPortalable = value;
                break;
            case Direction.Down:
                DownPortalable = value;
                break;
            case Direction.Left:
                LeftPortalable = value;
                break;
            case Direction.Right:
                RightPortalable = value;
                break;
            case Direction.Forward:
                FrontPortalable = value;
                break;
            case Direction.Back:
                BackPortalable = value;
                break;
        }
    }

    public readonly PuzzlemakerVoxel WithPortalability(Direction direction, bool value)
    {
        PuzzlemakerVoxel result = this;
        result.SetPortalability(direction, value);
        return result;
    }

    public readonly PuzzlemakerVoxel WithPortalability(Direction? direction, bool value)
    {
        PuzzlemakerVoxel result = this;
        if (direction.HasValue)
            result.SetPortalability(direction.Value, value);
        else
            result.SetPortalability(value);
        return result;
    }

    /// <summary>
    /// Set the portalability for all sides.
    /// </summary>
    /// <param name="value">Portalability</param>
    public void SetPortalability(bool value)
    {
        FrontPortalable = value;
        BackPortalable = value;
        UpPortalable = value;
        DownPortalable = value;
        LeftPortalable = value;
        RightPortalable = value;
    }

    /// <summary>
    /// Set the portalability for all sides.
    /// </summary>
    /// <param name="value">Portalability</param>
    /// <returns>A copy of this voxel with the set portalability.</returns>
    public readonly PuzzlemakerVoxel WithPortalability(bool value)
    {
        PuzzlemakerVoxel result = this;
        result.SetPortalability(value);
        return result;
    }
}
