using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Godot;
using PuzzlemakerPlus.Commands;
using PuzzlemakerPlus.Items;

namespace PuzzlemakerPlus;

/// <summary>
/// A puzzlemaker project loaded in memory.
/// </summary>
[GlobalClass]
public partial class PuzzlemakerProject : RefCounted
{
    public PuzzlemakerWorld World { get; }

    public CommandStack CommandStack { get; } = new();

    /// <summary>
    /// The file path at which this project is saved. Null if it's a new project that hasn't been saved yet.
    /// </summary>
    public string? FileName { get; set; }

    public Dictionary<string, Item> Items { get; } = new();

    public PuzzlemakerProject()
    {
        World = new PuzzlemakerWorld();
    }

    private PuzzlemakerProject(JsonObject json)
    {
        World = JsonSerializer.Deserialize<PuzzlemakerWorld>(json["World"], JsonUtils.JsonOptions) ?? new();
        
        JsonObject? items = json["Items"]?.AsObject();
        if (items != null)
        {
            foreach (var (id, v) in items)
            {
                if (id == null || v == null) continue;
                try
                {
                    JsonObject item = v.AsObject();
                    LoadItem(id, item, JsonUtils.JsonOptions);
                }
                catch (Exception e)
                {
                    GD.PushError($"Error loading item {id}: ", e);
                }
            }
        }

    }

    private JsonObject WriteJson()
    {
        JsonObject json = new JsonObject();
        json["World"] = JsonSerializer.SerializeToNode(World, JsonUtils.JsonOptions);

        JsonObject items = new JsonObject();

        foreach (var (id, item) in Items)
        {
            try
            {
                items[id] = SaveItem(item, JsonUtils.JsonOptions);
            }
            catch (Exception e)
            {
                GD.PushError($"Error saving item {id}: ", e);
            }
        }

        json["Items"] = items;
        return json;
    }

    protected Item LoadItem(string id, JsonObject json, JsonSerializerOptions options)
    {
        string typeStr = json["Type"]?.AsValue()?.GetValue<string>() ?? throw new JsonException("Missing item type");

        if (PackageManager.Instance.ItemTypes.TryGetValue(typeStr, out var itemType))
        {
            Item item = itemType.CreateInstance(this, id);
            item.ReadJson(json, options);
            Items.Add(id, item);
            return item;
        }
        else
            throw new InvalidOperationException($"Unknown item class {typeStr}.");
    }

    protected JsonObject SaveItem(Item item, JsonSerializerOptions options)
    {
        JsonObject json = new JsonObject();

        item.WriteJson(json, options);
        json["Type"] = item.Type.ID;
        return json;
    }

    /// <summary>
    /// Get an array of all the chunks in the world that contain voxels. Utility to call from GDScript.
    /// </summary>
    /// <returns>PackedVector3Array of occupied chunks.</returns>
    public Vector3[] GetOccupiedChunks()
    {
        return World.Chunks.Keys.Select(vec => new Vector3(vec.X, vec.Y, vec.Z)).ToArray();
    }


    public void WriteFile(Stream stream)
    {
        var json = WriteJson();
        JsonSerializer.Serialize(stream, json);
    }

    public static PuzzlemakerProject ReadFile(Stream stream)
    {
        //var json = JsonSerializer.Deserialize<PuzzlemakerProjectJson>(stream) ?? throw new Exception("Unable to read json");
        var json = JsonSerializer.Deserialize<JsonObject>(stream, JsonUtils.JsonOptions) ?? throw new Exception("Unable to read json");
        return new PuzzlemakerProject(json);
    }

}
