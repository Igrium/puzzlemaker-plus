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
    /// Draw a set of blocks from this world into a mesh.
    /// </summary>
    /// <param name="mesh">Mesh to add to.</param>
    /// <param name="chunk">The chunk coordinate of the chunk to render.</param>
    /// <param name="invert">If set, render the inside of the blocks instead of the outside.</param>
    public void RenderChunk(ArrayMesh mesh, Vector3I chunk, bool invert = true) 
    {
        RenderChunkAndCollision(mesh, null, chunk, invert);
    }

    /// <summary>
    /// Create render and collision geometry from a set of blocks from this world.
    /// </summary>
    /// <param name="mesh">Mesh to add render geometry to.</param>
    /// <param name="collision">Shape to add collision geometry to.</param>
    /// <param name="chunk">The chunk coordinate of the chunk to render</param>
    /// <param name="invert">If set, render the inside of the blocks instead of the outside.</param>
    public void RenderChunkAndCollision(ArrayMesh? mesh, ConcavePolygonShape3D? collision, Vector3I chunk, bool invert = true)
    {
        Quad[] quads = new GreedyMesh().DoGreedyMesh(this, chunk, uvScale: .25f, invert: invert).ToArray();
        
        if (mesh != null)
        {
            MultiMeshBuilder builder = new();
            for (int i = 0; i < quads.Length; i++)
            {
                builder.AddQuad(in quads[i]);
            }
            builder.ToMesh(mesh, EditorState.Instance.Theme.EditorMaterials);
        }

        if (collision != null)
        {
            PolygonShapeBuilder builder = new PolygonShapeBuilder();
            for (int i = 0; i < quads.Length; i++)
            {
                builder.AddQuad(in quads[i]);
            }
            builder.ToShape(collision);
        }
    }

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

    /// <summary>
    /// Read a voxel chunk from a base-64-encoded string.
    /// </summary>
    /// <param name="base64">Base-64 string.</param>
    /// <returns>The chunk.</returns>
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
    /// Encode a voxel chunk into a base-64-encoded string.
    /// </summary>
    /// <param name="chunk">The chunk.</param>
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
                //GD.Print($"Loaded {world.Chunks.Count} chunk(s) from json.");
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
