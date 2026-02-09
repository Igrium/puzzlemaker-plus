using Godot;
using Godot.Collections;

namespace PuzzlemakerPlus.Items;

[GlobalClass]
public sealed partial class ItemVariantThemeData : Resource
{
    /// <summary>
    /// The theme name(s) that this variant will apply to.
    /// Leave empty to signify that this should be used for all level themes.
    /// </summary>
    [Export]
    public Array<string> ThemeNames { get; set; } = new();
    
    /// <summary>
    /// If set, override the editor model with this while using this theme.
    /// </summary>
    [Export]
    public string? EditorModel { get; set; }

    /// <summary>
    /// The hammer instance that this theme variant will use.
    /// </summary>
    [Export]
    public string Instance { get; set; } = "";

    /// <summary>
    /// A list of voxels where antlines can spawn, relative to the object's root.
    /// The face of the voxel in question corresponds to the attachment direction of the item.
    /// </summary>
    [Export]
    public Array<Vector3I> AntlineConnections { get; set; } = new();

    /// <summary>
    /// A list of custom assets that need to be packed for this item.
    /// Will be searched in the editor's mounted resources, prepended with "res://package/game/"
    /// </summary>
    [Export]
    public Array<string> Assets { get; set; } = new();

    /// <summary>
    /// A list of transitive instance dependencies that the instance relies on.
    /// </summary>
    /// <remarks>
    /// I don't remember what I was intending with this field.
    /// </remarks>
    [Export]
    public Array<string> Dependencies { get; set; } = new();
}