using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace PuzzlemakerPlus;

[JsonConverter(typeof(LevelThemeJsonConverter))]
public sealed partial class LevelTheme : RefCounted
{
    // RefCounted doesn't work well with json serializer, so keep a separate instance for json.
    // This is really dumb...
    internal class Internal
    {
        public string? EditorTheme { get; set; } = null;
        public string[] WallTextures { get; set; } = Array.Empty<string>();
        public string[] FloorTextures { get; set; } = Array.Empty<string>();
        public string[] CeilingTextures { get; set; } = Array.Empty<string>();
    }

    readonly internal Internal _internal;

    public LevelTheme()
    {
        _internal = new();
    }

    internal LevelTheme(Internal @internal)
    {
        _internal = @internal;
    }

    /// <summary>
    /// Path to the editor theme to use. If null, use default editor theme.
    /// </summary>
    public string? EditorTheme { get => _internal.EditorTheme; set => _internal.EditorTheme = value; }

    public string[] WallTextures { get => _internal.WallTextures; set => _internal.WallTextures = value; }
    public string[] FloorTextures { get => _internal.FloorTextures; set => _internal.FloorTextures = value; }
    public string[] CeilingTextures { get => _internal.FloorTextures; set => _internal.FloorTextures = value; }

    /// <summary>
    /// Get the wall texture to use for a given portalability and subdivision.
    /// </summary>
    /// <param name="portalable">Portalability</param>
    /// <param name="subdiv">Subdivision level</param>
    /// <returns>The texture, or null if no texture could be found.</returns>
    public string? GetWallTexture(bool portalable, int subdiv)
    {
        return GetTexture(portalable, subdiv, WallTextures, true);
    }

    /// <summary>
    /// Get the floor texture to use for a given portalability and subdivision.
    /// </summary>
    /// <param name="portalable">Portalability</param>
    /// <param name="subdiv">Subdivision level</param>
    /// <returns>The texture, or null if no texture could be found.</returns>
    public string? GetFloorTexture(bool portalable, int subdiv)
    {
        return GetTexture(portalable, subdiv, FloorTextures, false) ?? GetWallTexture(portalable, subdiv);
    }

    /// <summary>
    /// Get the ceiling texture to use for a given portalability and subdivision.
    /// </summary>
    /// <param name="portalable">Portalability</param>
    /// <param name="subdiv">Subdivision level</param>
    /// <returns>The texture, or null if no texture could be found.</returns>
    public string? GetCeilingTexture(bool portalable, int subdiv)
    {
        return GetTexture(portalable, subdiv, CeilingTextures, false) ?? GetWallTexture(portalable, subdiv);
    }

    /// <summary>
    /// Get the editor texture to use for a given portalability and subdivision.
    /// </summary>
    /// <param name="portalable">Portalability.</param>
    /// <param name="subdiv">Subdivision.</param>
    /// <param name="textureList">List of textures to pull from.</param>
    /// <param name="clampSubdiv">If set and the subdivision is greater than the max subdivision count, the subdivision will be clamped.</param>
    /// <returns>The texture, or null if no texture could be found for that portalability and subdivision level.</returns>
    public static string? GetTexture(bool portalable, int subdiv, string[] textureList, bool clampSubdiv = true)
    {
        if (subdiv < 0)
            return null;

        int maxCount = textureList.Length;
        int maxSubdiv = maxCount / 2;
        if (subdiv > maxSubdiv && clampSubdiv)
            subdiv = maxSubdiv;

        int index = subdiv * 2 + (portalable ? 1 : 0);
        return index < maxCount ? textureList[index] : null;
    }

    /// <summary>
    /// Attempt to Load a level theme from a file.
    /// </summary>
    /// <param name="path">File path to Load from.</param>
    /// <returns>The level theme, or null if it could not be loaded.</returns>
    public static LevelTheme? Load(string path)
    {
        try
        {
            using (FileAccessStream stream = new FileAccessStream(path))
            {
                return JsonSerializer.Deserialize<LevelTheme>(stream, JsonUtils.JsonOptions);
            }
        }
        catch (JsonException e)
        {
            GD.PrintErr(e.Message); // No need to spam console with stack trace if it's the package dev's fault.
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
        return null;
    }
}

internal class LevelThemeJsonConverter : JsonConverter<LevelTheme>
{
    public override LevelTheme? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        LevelTheme result = new();
        LevelTheme.Internal? @internal = options.GetConverter<LevelTheme.Internal>().Read(ref reader, typeof(LevelTheme.Internal), options);
        return @internal != null ? new LevelTheme(@internal) : new LevelTheme();
    }

    public override void Write(Utf8JsonWriter writer, LevelTheme value, JsonSerializerOptions options)
    {
        options.GetConverter<LevelTheme.Internal>().Write(writer, value._internal, options);
    }
}
