using System.Collections.Generic;
using Godot;
using Godot.NativeInterop;

namespace PuzzlemakerPlus.Items;

/// <summary>
/// The rotation mode of the item. Rotations are always done on the axis of the mount direction.
/// </summary>
public enum RotationMode
{
    /// <summary>
    /// This item can't rotate.
    /// </summary>
    Fixed,
    /// <summary>
    /// This item can rotate in 90-degree increments.
    /// </summary>
    Quarter,
    /// <summary>
    /// This item can rotate any amount.
    /// </summary>
    Full
}

/// <summary>
/// A single item type such as a cube or a button.
/// One instance of this class will exist per item sta
/// </summary>
[GlobalClass]
public sealed partial class ItemType : Resource
{   
    [Export] 
    public string? ItemClassName { get; set; }

    public string Id { get; internal set; } = "";
    
    [Export]
    public Godot.Collections.Dictionary<string, ItemVariant> ItemVariants { get; set; } = new();

    /// <summary>
    /// The direction from which the item mounts to the wall or floor. Defaults to Direction.Down
    /// </summary>
    [Export]
    public Direction MountDirection { get; set; } = Direction.Down;

    [Export]
    public RotationMode RotationMode { get; set; } = RotationMode.Fixed;

    public string? GetEditorModel(string itemVariant, string? editorTheme)
    {
        return ItemVariants.GetValueOrDefault(itemVariant)?.GetEditorModel(editorTheme);
    }
}