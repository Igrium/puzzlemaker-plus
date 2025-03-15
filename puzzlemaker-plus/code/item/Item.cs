using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus.Items;

/// <summary>
/// An item within the editor. 
/// Each instance of an item will correspond to an instance of this class; for example, if there's two buttons in the map, two Items will exist.
/// The subclass of ItemPropHolder that's instansiated is determined by the ItemType and the item converterType's json.
/// </summary>
[GlobalClass]
public partial class Item : ItemPropHolder
{
    /// <summary>
    /// Called when the position of the item is updated.
    /// </summary>
    /// <param name="oldPos">The previous position.</param>
    /// <param name="newPos">The new position.</param>
    [Signal]
    public delegate void UpdatePositionEventHandler(Vector3 oldPos, Vector3 newPos);

    [Signal]
    public delegate void UpdateRotationEventHandler(Vector3 oldRot, Vector3 newRot);

    [Signal]
    public delegate void SetVariantEventHandler(string oldVal, string newVal);

    /// <summary>
    /// The ItemType this item is an instance of.
    /// </summary>
    public ItemType Type { get; }
    public PuzzlemakerProject Project { get; }

    /// <summary>
    /// A unique ID for the item that's used during serialization.
    /// </summary>
    public string ID { get; }

    public Item(ItemType type, PuzzlemakerProject project, string id)
    {
        Type = type;
        Project = project;
        ID = id;
    }

    private Vector3 _position;

    /// <summary>
    /// The position of the item in editor space.
    /// </summary>
    [ItemProp]
    public Vector3 Position
    {
        get => _position;
        set
        {
            Vector3 oldPos = _position;
            _position = value;
            EmitSignal(SignalName.UpdatePosition, oldPos, value);
        }
    }

    private Vector3 _rotation;

    /// <summary>
    /// The YXZ euler rotation of the item in radians
    /// </summary>
    [ItemProp]
    public Vector3 Rotation
    {
        get => _rotation;
        set
        {
            Vector3 oldRot = _rotation;
            _rotation = value;
            EmitSignal(SignalName.UpdateRotation, oldRot, value);
        }
    }

    /// <summary>
    /// The rotation of this item as a quaternion.
    /// </summary>
    public Quaternion RotationQuat
    {
        get => Quaternion.FromEuler(_rotation);
        set => Rotation = value.GetEuler();
    }

    private string _variant = "Standard";

    /// <summary>
    /// The name of the current item variant.
    /// </summary>
    [ItemProp]
    public string Variant
    {
        get => _variant;
        set
        {
            string oldVal = _variant;
            _variant = value;
            EmitSignal(SignalName.SetVariant, oldVal, value);
        }
    }

    /// <summary>
    /// A shortcut getter to retrieve the current variant instance from the type.
    /// </summary>
    public ItemVariant? ItemVariant => Type.Variants.GetValueOrDefault(_variant);

    /* GDScript Utility Functions */

    public string[] GetVariants()
    {
        return Type.Variants.Keys.ToArray();
    }

    /// <summary>
    /// Return a collection of all variant display names. For use in GDScript.
    /// </summary>
    /// <returns>Dictionary with variant IDs and their display names</returns>
    public virtual Godot.Collections.Dictionary<string, string> GetVariantDisplayNames()
    {
        Godot.Collections.Dictionary<string, string> dict = new();
        foreach (var (id, var) in Type.Variants)
        {
            dict[id] = var.DisplayName ?? id;
        }
        return dict;
    }

    public virtual string? GetEditorModel(string variant, string? editorTheme)
    {
        return Type.GetEditorModel(variant, editorTheme);
    }

    /// <summary>
    /// Get the current editor model. Make sure to call after changes to variant or theme.
    /// </summary>
    /// <param name="editorTheme">Theme to get the model for.</param>
    /// <returns>The resource path of the editor model. Null if there's something wrong and there is no model.</returns>
    public string? GetEditorModel(string? editorTheme)
    {
        return GetEditorModel(Variant, editorTheme);
    }

    /// <summary>
    /// Get the editor model for the current theme. Make sure to call after changes to variant or theme.
    /// </summary>
    /// <returns></returns>
    public string? GetEditorModel()
    {
        return GetEditorModel(Variant, EditorState.Instance.ThemeName);
    }

    public virtual Direction? GetMountDirection(string variant)
    {
        if (Type.Variants.TryGetValue(variant, out var value))
        {
            return value.MountDirection;
        }
        else return Direction.Down;
    }

    public Direction? GetMountDirection()
    {
        return GetMountDirection(Variant);
    }
}
