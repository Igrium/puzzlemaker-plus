using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Text.Json;
using System.Threading;
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
    public const string PackagePath = "packages";
    public const string UserPackagePath = "user://packages";
    public const string ItemsPath = "res://package/items";

    private static PackageManager? _instance;
    public static PackageManager Instance => _instance ?? throw new InvalidOperationException("Package manager hasn't finished initializing!");

    /// <summary>
    /// Called when all packages have finished loading.
    /// </summary>
    /// <param name="initial">If this is the first time the packages have loaded.</param>
    [Signal]
    public delegate void PackagesReloadedEventHandler(bool initial);

    public PackageManager()
    {
        if (_instance != null)
            GD.PushWarning("Tried to initialize PackageManager twice!");
        _instance = this;
    }

    public IDictionary<string, ItemType> ItemTypes { get; } = new ConcurrentDictionary<string, ItemType>();

    private bool _initialPackageLoad = true;

    public override void _Ready()
    {
        base._Ready();
        LoadPackages();
    }

    public async void LoadPackages()
    {
        bool initial = _initialPackageLoad;

        GD.Print("Scanning packages...");
        string[] packages = await Task.Run(ScanPackages);
        GD.Print($"Discovered {packages.Length} package(s).");

        int successCount = 0;
        int failedCount = 0;

        foreach (var pkg in packages)
        {
            if (ProjectSettings.LoadResourcePack(pkg))
            {
                successCount++;
                GD.Print("Mounted " + pkg);
            }
            else
            {
                failedCount++;
                GD.PushError("Failed to mount " + pkg);
            }
        }

        if (successCount > 0)
            GD.Print($"Mounted {successCount} package(s).");
        if (failedCount > 0)
            GD.PrintErr($"{failedCount} package(s) failed to mount.");

        GD.Print("Loading items...");
        await LoadItemTypes();

        _initialPackageLoad = false;
        EmitSignal(SignalName.PackagesReloaded, _initialPackageLoad);
    }

    public string[] ScanPackages()
    {
        return ScanPackages(PackagePath).Concat(ScanPackages(UserPackagePath)).ToArray();
    }

    private string[] ScanPackages(string path)
    {
        if (!DirAccess.DirExistsAbsolute(path))
            return Array.Empty<string>();

        using var dir = DirAccess.Open(path);
        if (dir == null)
        {
            GD.PushError("Error scanning packages: " + DirAccess.GetOpenError());
            return Array.Empty<string>();
        }

        return dir.GetFiles()
            .Where(filename => filename.EndsWith(".zip") || filename.EndsWith(".pck"))
            .Concat(dir.GetDirectories())
            .Select(filename => path.PathJoin(filename))
            .ToArray();
    }

    public Task LoadItemTypes()
    {
        ItemTypes.Clear();
        string[] resources = ResourceLoader.ListDirectory(ItemsPath).Where(name => name.EndsWith(".json")).ToArray();

        List<Task> tasks = new(resources.Length);
        foreach (string resource in resources)
        {
            Task.Run(() => LoadItemType(ItemsPath.PathJoin(resource)));
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
            itemType.ID = name;

            if (ItemTypes.ContainsKey(name))
            {
                GD.PushWarning("Duplicate item type: " + name);
            }

            ItemTypes[name] = itemType;
            GD.Print("Loaded " + resourcePath);
            return itemType;
        }
        // Don't flood the console with stack trace if it's the package dev's fault.
        catch (JsonException e)
        {
            GD.PrintErr($"Unable to load json for {resourcePath}: ", e.Message);
        }
        catch (Exception e)
        {
            GD.PushError($"Unable to load {resourcePath}: ", e);

        }
        return null;
    }

    protected static void PrintError(string message, Exception ex)
    {
        if (OS.HasFeature("editor"))
        {
            GD.PushError(message, ex);
            GD.PrintErr(message + ": " + ex.Message);
        }
        else
        {
            GD.PushError(message + ": ", ex.Message);
        }
    }

    /// <summary>
    /// Load a texture from the packages. Load imported resource if there is one; otherwise, load from file.
    /// </summary>
    /// <param name="resourcePath">Resource path. Should start with 'res://'</param>
    /// <returns>The texture, or null if it could not be loaded.</returns>
    public static Texture2D? LoadImageTexture(string resourcePath)
    {
        if (ResourceLoader.Exists(resourcePath, "Texture2D"))
        {
            Resource res = ResourceLoader.Load(resourcePath, "Texture2D");
            if (res is Texture2D tex)
                return tex;
        }


        var image = Image.LoadFromFile(resourcePath);
        if (image == null)
        {
            GD.PushError("Unable to load image from " + resourcePath);
            return null;
        }
        return ImageTexture.CreateFromImage(image);

    }

    /// <summary>
    /// Load a model as a packed scene from the packages. Load imported resource if there is one; otherwise, load from file.
    /// </summary>
    /// <param name="resourcePath">Resourece path. Should start with 'res://'</param>
    /// <returns>The model scene, or null if it could not be loaded.</returns>
    public static PackedScene? LoadModel(string resourcePath)
    {
        if (ResourceLoader.Exists(resourcePath, "PackedScene"))
        {
            Resource res = ResourceLoader.Load(resourcePath, "PackedScene");
            if (res is PackedScene scene)
                return scene;
        }

        var gltfDocument = new GltfDocument();
        var gltfState = new GltfState();

        var error = gltfDocument.AppendFromFile(resourcePath, gltfState);
        if (error != Error.Ok)
        {
            GD.PushError($"Couldn't load glTF scene (error code: {error}).");
            return null;
        }

        // We need to dispose of the node manually because it's not being put in a scene.
        using (Node node = gltfDocument.GenerateScene(gltfState))
        {
            PackedScene scene = new PackedScene();
            error = scene.Pack(node);
            if (error != Error.Ok)
            {
                GD.PushError($"Couldn't pack gltf scene (error code: {error}.");
                return null;
            }
            return scene;
        }
    }
}
