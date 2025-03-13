using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    /// <summary>
    /// Called after an item has been added to the project.
    /// </summary>
    /// <param name="item">The item instance.</param>
    [Signal]
    public delegate void ItemAddedEventHandler(Item item);

    /// <summary>
    /// Called after an item has been removed from the project.
    /// </summary>
    /// <param name="item">The item. Could still be referenced in places such as the undo stack.</param>
    [Signal]
    public delegate void ItemRemovedEventHandler(Item item);

    public PuzzlemakerWorld World { get; }

    public CommandStack CommandStack { get; } = new();

    /// <summary>
    /// The file path at which this project is saved. Null if it's a new project that hasn't been saved yet.
    /// </summary>
    public string? FileName { get; set; }

    private Dictionary<string, Item> _items = new();

    public IReadOnlyDictionary<string, Item> Items { get => _items; }

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
            AddItem(item);
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
    /// Add an item to this project.
    /// </summary>
    /// <param name="item">Item to add.</param>
    /// <returns>If the item's ID didn't conflict with another item's ID and it was added.</returns>
    /// <exception cref="ArgumentException">If the item belongs to the wrong project.</exception>
    public virtual bool AddItem(Item item)
    {
        if (item.Project != this)
            throw new ArgumentException("Item belongs to the wrong project.", nameof(item));

        if (!_items.TryAdd(item.ID, item))
        {
            GD.PushWarning($"An item already exists with ID {item.ID}. Ignoring.");
            return false;
        }
        EmitSignal(SignalName.ItemAdded, item);
        return true;
    }

    /// <summary>
    /// Attempt to remove an item from this project.
    /// </summary>
    /// <param name="item">Item to remove.</param>
    /// <returns>If the item was found.</returns>
    public virtual bool RemoveItem(Item item)
    {
        if (_items.Remove(item.ID))
        {
            EmitSignal(SignalName.ItemRemoved, item);
            return true;
        }
        else return false;
    }

    /// <summary>
    /// Attempt to remove an item from this project by its ID.
    /// </summary>
    /// <param name="id">ID of item to remove.</param>
    /// <returns>If the item was found.</returns>
    public bool RemoveItem(string id)
    {
        if (_items.TryGetValue(id, out var item))
        {
            return RemoveItem(item);
        }
        else return false;
    }

    /// <summary>
    /// Create an item and add it to this project.
    /// </summary>
    /// <param name="type">Item type to instantiate.</param>
    /// <param name="id">ID to give it. Null for automatic ID.</param>
    /// <returns>The item, or null if there was an error stopping it from being created.</returns>
    public Item? CreateItem(ItemType type, string? id = null)
    {
        if (id == null)
            id = GetEmptyItemID();

        Item item;
        try
        {
            item = type.CreateInstance(this, id);
        }
        catch (Exception e)
        {
            GD.PushError($"Error instantiating item {type.ID}: ", e);
            return null;
        }

        return AddItem(item) ? item : null;
    }

    /// <summary>
    /// Get all the items in this project as a godot array.
    /// </summary>
    /// <returns>Item array. This array is a duplicate; changes to the item set will NOT reflect here, and vice-versa.</returns>
    public Godot.Collections.Array<Item> GetItems()
    {
        var items = new Godot.Collections.Array<Item>();
        items.AddRange(Items.Values);
        return items;
    }

    public string[] GetItemIDs()
    {
        return Items.Keys.ToArray();
    }

    public string GetEmptyItemID()
    {
        int count = Items.Count;
        string id = "item-" + count;
        while (Items.ContainsKey(id))
        {
            count++;
            id = "item-" + count;
        }
        return id;
    }

    /// <summary>
    /// Get an array of all the chunks in the world that contain voxels. Utility to call from GDScript.
    /// </summary>
    /// <returns>PackedVector3Array of occupied chunks.</returns>
    public Vector3[] GetOccupiedChunks()
    {
        return World.Chunks.Keys.Select(vec => new Vector3(vec.X, vec.Y, vec.Z)).ToArray();
    }

    /// <summary>
    /// Add an initial set of voxels to the world so editing can commence.
    /// </summary>
    public void AddInitialVoxels()
    {
        Vector3I min = new Vector3I(-16, 0, -16);
        Vector3I max = new Vector3I(15, 7, 15);
        World.Fill(min, max, new PuzzlemakerVoxel() { IsOpen = true });

        EditorState.Instance.EmitOnChunksUpdated(min.GetChunk(), max.GetChunk());
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
