using System;
using Godot;
using Godot.Collections;

namespace PuzzlemakerPlus;

/// <summary>
/// Contains textures and other visual elements to use for the editor.
/// Editor themes can be set by the level theme. If unset, they revert to a default.
/// </summary>
[GlobalClass]
public partial class EditorTheme : Resource
{
    public const string DEFAULT = "res://assets/editor_themes/clean.tres";

    /// <summary>
    /// The voxel textures that this theme will use.
    /// Textures are defined using the pattern:
    /// [subdiv0-noportal, subdiv0-portalable, subdiv1-noportal, subdiv1-portalable, ...]
    /// </summary>
    [Export]
    public Array<Material> WallTextures { get; set; } = new();

    /// <summary>
    /// Voxel textures to use on the floor. If empty, use WallTextures.
    /// </summary>
    [Export]
    public Array<Material> FloorTextures { get; set; } = new();

    /// <summary>
    /// Voxel textures to use on the ceiling. If empty, use WallTextures.
    /// </summary>
    [Export]
    public Array<Material> CeilingTextures { get; set; } = new();

    /// <summary>
    /// Get the editor texture to use for a given portalability and subdivision.
    /// </summary>
    /// <param name="portalable">Portalability</param>
    /// <param name="subdiv">Subdivision level</param>
    /// <returns>The texture, or null if no texture could be found.</returns>
    public Material? GetWallTexture(bool portalable, int subdiv)
    {
        return GetEditorTexture(portalable, subdiv, WallTextures, true);
    }

    /// <summary>
    /// Get the floor texture to use for a given portalability and subdivision.
    /// </summary>
    /// <param name="portalable">Portalability</param>
    /// <param name="subdiv">Subdivision level</param>
    /// <returns>The texture, or null if no texture could be found.</returns>
    public Material? GetFloorTexture(bool portalable, int subdiv)
    {
        return GetEditorTexture(portalable, subdiv, FloorTextures, false) ?? GetWallTexture(portalable, subdiv);
    }

    /// <summary>
    /// Get the ceiling texture to use for a given portalability and subdivision.
    /// </summary>
    /// <param name="portalable">Portalability</param>
    /// <param name="subdiv">Subdivision level</param>
    /// <returns>The texture, or null if no texture could be found.</returns>
    public Material? GetCeilingTexture(bool portalable, int subdiv)
    {
        return GetEditorTexture(portalable, subdiv, CeilingTextures, false) ?? GetWallTexture(portalable, subdiv);
    }

    /// <summary>
    /// Get the editor texture to use for a given portalability and subdivision.
    /// </summary>
    /// <param name="portalable">Portalability.</param>
    /// <param name="subdiv">Subdivision.</param>
    /// <param name="textureList">List of textures to pull from.</param>
    /// <param name="clampSubdiv">If set and the subdivision is greater than the max subdivision count, the subdivision will be clamped.</param>
    /// <returns>The texture, or null if no texture could be found for that portalability and subdivision level.</returns>
    public static Material? GetEditorTexture(bool portalable, int subdiv, Array<Material> textureList, bool clampSubdiv = true)
    {
        if (subdiv < 0)
            return null;

        int maxCount = textureList.Count;
        int maxSubdiv = maxCount / 2;
        if (subdiv > maxSubdiv && clampSubdiv)
            subdiv = maxSubdiv;

        int index = subdiv * 2 + (portalable ? 1 : 0);
        return index < maxCount ? textureList[index] : null;
    }
}
