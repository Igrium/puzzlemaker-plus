using System.Linq;
using Godot;
using Godot.Collections;

namespace PuzzlemakerPlus.Items;

[GlobalClass]
public sealed partial class ItemVariant : Resource
{
    /// <summary>
    /// The display name of the variant. If unset, fallback to the variant ID.
    /// </summary>
    [Export] 
    public string? DisplayName { get; set; }

    [Export]
    public Array<ItemVariantThemeData> Themes { get; set; } = new();
    
    [Export]
    public string EditorModel { get; set; } = "";

    /// <summary>
    /// Return the variant theme that should be used for a given level theme.
    /// First all the variant themes are checked for one with a matching name, in order of least names to most names.
    /// If one isn't found, than the first theme in the list is selected.
    /// </summary>
    /// <param name="themeName">Level theme name.</param>
    /// <returns>The variant theme, or null if the package dev messed up and didn't specify any theme entries.</returns>
    public ItemVariantThemeData? GetThemeData(string themeName)
    {
        var theme = Themes.Where(theme => theme.ThemeNames.Contains(themeName))
            .OrderBy(theme => theme.ThemeNames.Count())
            .FirstOrDefault();
        return theme ?? Themes.FirstOrDefault();
    }

    /// <summary>
    /// GetVoxel the editor model to use for a given theme.
    /// </summary>
    /// <param name="themeName">Theme to get the model for. Null to get the default model.</param>
    /// <returns>The model, or null if the package dev messed up and no model was found.</returns>
    public string? GetEditorModel(string? themeName)
    {
        if (themeName == null)
            return EditorModel;
        
        string? model = GetThemeData(themeName)?.EditorModel;
        return string.IsNullOrWhiteSpace(model) ? EditorModel : model;
    }
}