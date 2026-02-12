using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;

namespace PuzzlemakerPlus.Items;

public partial class Item(PuzzlemakerProject project, string id) : RefCounted
{
    public PuzzlemakerProject Project { get; } = project;

    public string Id { get; } = id;

    [Signal]
    public delegate void UpdatePositionEventHandler(Vector3 oldPos, Vector3 newPos);
    
    [Signal]
    public delegate void UpdateRotationEventHandler(Vector3 oldRot, Vector3 newRot);

    [Signal]
    public delegate void SetPropEventHandler(string propName, Variant oldVal, Variant newVal);

    /// <summary>
    /// Called when a state update triggers the editor model to be replaced.
    /// </summary>
    [Signal]
    public delegate void WantRefreshModelEventHandler();
    
    public ItemType Type { get; set; } = new ItemType();

    private Vector3 _position;

    public Vector3 Position
    {
        get => _position;
        set
        {
            var old = _position;
            _position = value;
            EmitSignalUpdatePosition(old, value);
        }
    }

    private Vector3 _rotation;

    public Vector3 Rotation
    {
        get => _rotation;
        set
        {
            var old = _rotation;
            _rotation = value;
            EmitSignalUpdateRotation(old, value);
        }
    }
    
    private readonly Dictionary<string, object> _props = new();
    
    public IReadOnlyDictionary<string, object> Props => _props;
    
    public Variant GetProperty(string propName)
    {
        if (Props.TryGetValue(propName, out var prop))
        {
            if (AsVariant(prop, out var val))
                return val;
        }
        return default;
    }

    public void SetProperty(string propName, Variant val)
    {
        var old = Props.GetValueOrDefault(propName);
        _props[propName] = val;
        AsVariant(old, out var oldVar);
        EmitSignalSetProp(propName, oldVar, val);
        EmitSignalWantRefreshModel();
        // EmitSignalUpdateModel(GetEd);
    }

    public void SetProperty(String propName, object val)
    {
        var old = Props.GetValueOrDefault(propName);
        _props[propName] = val;
        AsVariant(old, out var oldVar);
        EmitSignalSetProp(propName, oldVar, default);
        EmitSignalWantRefreshModel();
    }

    public string? GetEditorModel(string theme)
    {
        return Type.GetEditorModel(Props, theme);
    }

    private static bool AsVariant(object? value, out Variant variant)
    {
        try
        {
            variant = value != null ? (Variant)value : default;
            return true;
        }
        catch (InvalidCastException)
        {
            variant = default;
            return false;
        }
    }
}