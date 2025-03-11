using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using PuzzlemakerPlus.Items;

namespace PuzzlemakerPlus;

/// <summary>
/// Loads item packages from disk.
/// </summary>
[GlobalClass]
public partial class PackageManager : Node
{
    public const string ItemsPath = "res://items";

    private static PackageManager? _instance;
    public static PackageManager Instance => _instance ?? throw new InvalidOperationException("Package manager hasn't finished initializing!");

    /// <summary>
    /// Called when all packages have finished loading.
    /// </summary>
    [Signal]
    public delegate void PackagesLoadedEventHandler();

    public PackageManager()
    {
        if (_instance != null)
            GD.PushWarning("Tried to initialize PackageManager twice!");
        _instance = this;
    }

    public IDictionary<string, ItemType> ItemTypes { get; } = new ConcurrentDictionary<string, ItemType>();

    public Task LoadItemTypes()
    {
        string[] resources = ResourceLoader.ListDirectory(ItemsPath).Where(name => name.EndsWith(".json")).ToArray();

        List<Task> tasks = new(resources.Length);
        foreach (string resource in resources)
        {
            Task.Run(() => LoadItemType(resource));
        }

        return Task.WhenAll(tasks);
    }

    private ItemType? LoadItemType(string resourcePath)
    {
        string name = Path.GetFileNameWithoutExtension(resourcePath);
        try
        {
            ItemType itemType;
            using (var stream = new FileAccessStream(resourcePath))
            {
                itemType = JsonSerializer.Deserialize<ItemType>(stream, JsonUtils.JsonOptions) ?? throw new Exception("Item type didn't load.");
            }
            
            if (ItemTypes.ContainsKey(name))
            {
                GD.PushWarning("Duplicate item type: " + name);
            }

            ItemTypes[name] = itemType;
            GD.Print("Loaded " + resourcePath);
            return itemType;
        }
        catch (Exception e)
        {
            GD.PushError($"Error loading item type {name}:", e);
            return null;
        }
    }
}
