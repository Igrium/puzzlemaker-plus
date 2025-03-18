using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus.Items;

/// <summary>
/// A single item type such as a cube or a button.
/// One instance of this class will exist per item json in the package.
/// </summary>
public sealed class ItemType
{
    public string ItemClassName { get; set; } = "Item";

    [JsonIgnore]
    public string ID { get; internal set; } = "";

    public Dictionary<string, ItemVariant> Variants { get; set; } = new Dictionary<string, ItemVariant>();

    /// <summary>
    /// GetVoxel the editor model to use for a given variant and theme.
    /// </summary>
    /// <param name="variant">Variant ID to use.</param>
    /// <param name="editorTheme">Editor theme to use. Null to use default theme.</param>
    /// <returns>The model path; null if there is no variant by that name or no model was declared.</returns>
    public string? GetEditorModel(string variant, string? editorTheme)
    {
        if (Variants.TryGetValue(variant, out var v))
            return v.GetEditorModel(editorTheme);
        else
            return null;
    }

    /// <summary>
    /// Create an empty instance of this item type.
    /// </summary>
    /// <param name="project">Project to create the instance in. Does not actually add it to the project.</param>
    /// <param name="id">Item ID to assign.</param>
    /// <returns>The new item instance.</returns>
    /// <remarks>This could throw all sorts of exceptions. Best to try and catch them.</remarks>
    public Item CreateInstance(PuzzlemakerProject project, string id)
    {
        return ItemClasses.CreateInstance(ItemClassName, this, project, id);
    }
}

public sealed class ItemVariant
{
    /// <summary>
    /// The display name of the variant. If unset, fallback to the variant ID.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The wall that the item is "attached" to. If null, the item is considered floating.
    /// </summary>
    public Direction? MountDirection { get; set; } = Direction.Down;

    public List<ItemVariantTheme> Themes { get; set; } = new();

    [JsonConverter(typeof(DictOrValueJsonConverter<string>))]
    public Dictionary<string, string> EditorModel { get; set; } = new();

    /// <summary>
    /// GetVoxel the editor model to use for a given theme.
    /// </summary>
    /// <param name="editorTheme">Editor theme to use. Null to use the default model.</param>
    /// <returns>The model, or null if the package dev messed up and no model was found.</returns>
    public string? GetEditorModel(string? editorTheme)
    {
        if (editorTheme == null) editorTheme = "default";

        if (EditorModel.TryGetValue(editorTheme, out var model))
            return model;
        else if (EditorModel.TryGetValue("default", out model))
            return model;
        else return null;
    }

    /// <summary>
    /// Return the variant theme that should be used for a given level theme.
    /// First all the variant themes are checked for one with a matching name, in order of least names to most names.
    /// If one isn't found, than the first theme in the list is selected.
    /// </summary>
    /// <param name="levelTheme">Level theme name.</param>
    /// <returns>The variant theme, or null if the package dev messed up and didn't specify any theme entries.</returns>
    public ItemVariantTheme? GetVariantTheme(string levelTheme)
    {
        var theme = Themes.Where(theme => theme.Names.Contains(levelTheme)).OrderBy(theme => theme.Names.Count()).FirstOrDefault();
        return theme ?? Themes.FirstOrDefault();
    }
}

public sealed class ItemVariantTheme
{
    /// <summary>
    /// The theme name(s) that this variant will apply to. Leave empty to signify that this should be used for all level themes.
    /// </summary>
    public List<string> Names { get; } = new();

    /// <summary>
    /// The VMF instance that this theme variant will use.
    /// </summary>
    public string Instance { get; set; } = "";

    /// <summary>
    /// A list of voxels where antlines can spawn, relative to the object's root.
    /// The face of the voxel in question corrisponds to the attachment direction of the item.
    /// </summary>
    public List<Vector3I> AntlineConnections { get; } = new();

    /// <summary>
    /// A list of custom assets that need to be packed for this item.
    /// Will be searched in the editor's mounted resources, prepended with "res://package/game/"
    /// </summary>
    public List<string> Assets { get; } = new();

    /// <summary>
    /// A list of transitive instance dependencies that the instance relies on.
    /// </summary>
    public List<string> Dependencies { get; } = new();
}