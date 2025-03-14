using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace PuzzlemakerPlus;

public partial class LevelTheme : RefCounted
{
    /// <summary>
    /// The material to use for rendering voxels.
    /// </summary>
    [Export]
    [JsonIgnore]
    public Material VoxelMaterial { get; set; } = ResourceLoader.Load<Material>("res://assets/materials/editor_voxels.tres");

    /// <summary>
    /// Asset paths for textures to use in the editor.
    /// </summary>
    [Export]
    public string[] EditorTextures { get; set; } = new string[0];

    /// <summary>
    /// Deserialized godot materials for editor voxels. Use LoadMaterials() to load them.
    /// </summary>
    [JsonIgnore]
    public Material[] EditorMaterials { get; private set; } = new Material[0];

    /// <summary>
    /// Load all voxel textures from their respective paths. Call after updating EditorTextures or VoxelMaterial.
    /// </summary>
    public void LoadMaterials()
    {
        EditorMaterials = new Material[EditorTextures.Length];
        for (int i = 0; i < EditorTextures.Length; i++)
        {
            string path = EditorTextures[i];
            Texture2D tex;
            try
            {
                tex = ResourceLoader.Load<Texture2D>(path);
            }
            catch (Exception e)
            {
                GD.PushError($"Unable to load editor texture '{path}'", e);
                continue;
            }

            Material mat = (Material)VoxelMaterial.Duplicate();
            mat.Set("shader_parameter/albedo_texture", tex);
            EditorMaterials[i] = mat;
        }
    }

    /// <summary>
    /// Get the texture to use for a given portalability and subdivision level.
    /// </summary>
    /// <param name="portalable">Portalability.</param>
    /// <param name="subdiv">Subdivision level. 0 = 128 hammer units; 1 = 64 hammer units; 2 = 32 hammer units.</param>
    /// <returns>The editor material for this face. Null if none was found.</returns>
    public Material? GetEditorTexture(bool portalable, int subdiv)
    {
        if (subdiv < 0)
            throw new ArgumentOutOfRangeException(nameof(subdiv));

        int maxCount = EditorMaterials.Length;
        int maxSubdiv = maxCount / 2;
        if (subdiv > maxSubdiv)
            subdiv = maxSubdiv;

        int index = subdiv * 2 + (portalable ? 1 : 0);
        if (index >= maxCount)
        {
            GD.PushError($"Unable to get voxel texture for portalability {portalable} with subdivision {subdiv}.");
            return null;
        }

        return EditorMaterials[index];
    }

    /// <summary>
    /// Source engine paths for textures to use in-game
    /// </summary>
    [Export]
    public string[] VoxelTextures { get; set; } = new string[0];

    public string? GetVoxelTexture(bool portalable, int subdiv)
    {
        if (subdiv < 0)
            throw new ArgumentOutOfRangeException(nameof(subdiv));

        int maxCount = VoxelTextures.Length;
        int maxSubdiv = maxCount / 2;
        if (subdiv > maxSubdiv)
            subdiv = maxSubdiv;

        int index = subdiv * 2 + (portalable ? 1 : 0);
        if (index >= maxCount)
        {
            GD.PushError($"Unable to get voxel texture for portalability {portalable} with subdivision {subdiv}.");
            return null;
        }

        return VoxelTextures[index];
    }

    /// <summary>
    /// Attempt to load a level theme from the given resource path.
    /// </summary>
    /// <param name="path">Path to load from.</param>
    /// <returns>The theme, or null if the theme couldn't be loaded.</returns>
    public static LevelTheme? LoadTheme(string path)
    {
        GD.Print($"Loading theme: '{path}'");
        LevelTheme? theme;
        using (var file = FileAccess.Open(path, FileAccess.ModeFlags.Read))
        {
            if (file == null)
            {
                GD.PushError("Unable to load theme. ", FileAccess.GetOpenError());
                return null;
            }
            string json = file.GetAsText();
            try
            {
                theme = FromJson(json);
            }
            catch (Exception e)
            {
                GD.PushError("Unable to load theme. ", e);
                return null;
            }
        }

        theme?.LoadMaterials();
        return theme;
    }

    public static LevelTheme? FromJson(string json)
    {
        return JsonSerializer.Deserialize<LevelTheme>(json);
    }

    public static string ToJson(LevelTheme theme)
    {
        return JsonSerializer.Serialize(theme);
    }
}
