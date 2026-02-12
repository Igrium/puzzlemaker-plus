using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;

namespace PuzzlemakerPlus.Items;

public partial class Item(ItemType type) : RefCounted
{

    [Signal]
    public delegate void UpdatePositionEventHandler(Vector3 oldPos, Vector3 newPos);
    
    [Signal]
    public delegate void UpdateRotationEventHandler(Vector3 oldRot, Vector3 newRot);

    /// <summary>
    /// Called when a state update triggers the editor model to be replaced.
    /// </summary>
    [Signal]
    public delegate void RefreshModelEventHandler();

    public ItemType Type { get; } = type;
    
    // public ItemType Type { get; set; } = new ItemType();

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

    public Quaternion RotationQuat
    {
        get => Quaternion.FromEuler(Rotation);
        set => Rotation = value.GetEuler();
    }
    
    private readonly Dictionary<string, string> _properties = new();
    
    public IReadOnlyDictionary<string, string> Properties => _properties;

    public string? GetProperty(string propName)
    {
        return Properties.GetValueOrDefault(propName);
    }

    public void SetProperty(string propName, string value)
    {
        _properties[propName] = value;
        EmitSignalRefreshModel();
    }

    public void ResetProperty(string propName)
    {
        var val = DefaultPropValue(propName);
        if (val != null)
            _properties[propName] = val;
        else
            _properties.Remove(propName);
    }

    public void GetAllProperties(Godot.Collections.Dictionary<string, string> dest)
    {
        foreach (var (name, value) in Properties)
        {
            dest[name] = value;
        }
    }
    
    /// <summary>
    /// Get the default value of a property
    /// </summary>
    /// <param name="propName">The property's name</param>
    /// <returns>The default value, if defined.</returns>
    public string? DefaultPropValue(string propName)
    {
        return Type.PropNames.GetValueOrDefault(propName)?.DefaultValue;
    }

    /// <summary>
    /// Read a set of properties from a json object and apply them to this item.
    /// </summary>
    /// <param name="json">Json object to read from.</param>
    /// <param name="options">Json serialization options to use</param>
    public virtual void FromJson(JsonObject json, JsonSerializerOptions options)
    {
        // TODO: Load type
        if (json.TryGetPropertyValue("pos", out var posNode))
            _position = posNode.Deserialize<Vector3>(options);

        if (json.TryGetPropertyValue("rot", out var rotNode))
            _rotation = rotNode.Deserialize<Vector3>(options);

        _properties.Clear();
        if (json.TryGetPropertyValue("props", out var propsNode) && propsNode is JsonObject propsObj)
        {
            foreach (var (prop, val) in propsObj)
            {
                if (val != null)
                    _properties[prop] = val.ToString();
            }
        }
        FillDefaultProps();
        EmitSignalRefreshModel();
    }

    /// <summary>
    /// Serialize this item into json.
    /// </summary>
    /// <param name="json">Json object to write to</param>
    /// <param name="options">Json serialization options to use</param>
    public virtual void ToJson(JsonObject json, JsonSerializerOptions options)
    {
        // TODO: Save type
        json["pos"] = JsonSerializer.Serialize(_position, options);
        json["rot"] = JsonSerializer.Serialize(_rotation, options);

        JsonObject propsObj = new JsonObject();
        foreach (var (prop, val) in Properties)
        {
            propsObj[prop] = val;
        }
        json["props"] = propsObj;
    }

    private void FillDefaultProps()
    {
        foreach (var (name, def) in Type.PropNames)
        {
            if (def.DefaultValue != null && !Properties.ContainsKey(name))
            {
                _properties[name] = def.DefaultValue;
            }
        }
    }
    
}