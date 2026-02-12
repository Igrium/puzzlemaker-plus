using Godot;

namespace PuzzlemakerPlus.Items;

/// <summary>
/// A simple wrapper around ItemType to make it accessible from GDScript
/// </summary>
[GlobalClass]
public partial class ItemTypeProxy(ItemType type) : RefCounted
{
    public ItemType Type { get; } = type;
    public string Id => Type.Id;

    public string Thumbnail
    {
        get => Type.Thumbnail;
        set => type.Thumbnail = value;
    }

    public RotationMode RotationMode
    {
        get => type.RotationMode;
        set => type.RotationMode = value;
    }
    
}