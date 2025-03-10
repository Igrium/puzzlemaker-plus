using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Godot;
using Godot.NativeInterop;


namespace PuzzlemakerPlus.Item;

/// <summary>
/// An item within the editor. 
/// Each instance of an item will correspond to an instance of this class; for example, if there's two buttons in the map, two Items will exist.
/// The subclass of Item that's instansiated is determined by the ItemType and the item converterType's json.
/// </summary>
public partial class Item : RefCounted
{
    /// <summary>
    /// The ItemType this item is an instance of.
    /// </summary>
    public ItemType Type { get; }

    public Item(ItemType type)
    {
        this.Type = type;
    }

    [ItemProp]
    public Vector3 Position { get; set; }

    private readonly BindingFlags _bindingFlags = BindingFlags.Instance | BindingFlags.FlattenHierarchy;

    /// <summary>
    /// Read a set of properties from a json object and apply them to this item.
    /// </summary>
    /// <param name="json">Json object to read from.</param>
    public virtual void ReadJson(JsonObject json, JsonSerializerOptions options)
    {
        foreach (PropertyInfo prop in this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.FlattenHierarchy))
        {
            ItemProp? itemProp = prop.GetCustomAttribute<ItemProp>();
            if (itemProp == null)
                continue;

            if (json.TryGetPropertyValue(prop.Name, out var node))
            {
                JsonSerializerOptions localOptions = options;
                JsonConverterAttribute? jsonConverter = prop.GetCustomAttribute<JsonConverterAttribute>();
                if (jsonConverter != null)
                {
                    localOptions = new JsonSerializerOptions(options);
                    var converter = jsonConverter.CreateConverter(prop.PropertyType);
                    if (converter != null)
                        localOptions.Converters.Add(converter);
                }
                
                prop.SetValue(this, JsonSerializer.Deserialize(node, prop.PropertyType, localOptions));
            }
            
        }
    }

    /// <summary>
    /// Write all the properties from this item into a json object.
    /// </summary>
    /// <param name="json">Json object to write to.</param>
    public virtual void WriteJson(JsonObject json, JsonSerializerOptions options)
    {
        foreach (PropertyInfo prop in this.GetType().GetProperties(_bindingFlags))
        {
            ItemProp? itemProp = prop.GetCustomAttribute<ItemProp>();
            if (itemProp == null)
                continue;

            JsonSerializerOptions localOptions = options;
            JsonConverterAttribute? jsonConverter = prop.GetCustomAttribute<JsonConverterAttribute>();
            if (jsonConverter != null)
            {
                localOptions = new JsonSerializerOptions(options);
                var converter = jsonConverter.CreateConverter(prop.PropertyType);
                if (converter != null)
                    localOptions.Converters.Add(converter);
            }

            JsonNode? node = JsonSerializer.SerializeToNode(prop.GetValue(json, null), localOptions);

            json[prop.Name] = node;
        }
    }

    /// <summary>
    /// Get the display name of a property.
    /// </summary>
    /// <param name="propName">Property ID.</param>
    /// <returns>The display name; null if no property with that name exists.</returns>
    public string? GetPropertyDisplayName(string propName)
    {
        PropertyInfo? prop = GetType().GetProperty(propName, _bindingFlags);
        return ItemProp.GetPropertyDisplayName(prop);
    }

    /// <summary>
    /// Get the display type of a property.
    /// </summary>
    /// <param name="propName">Property ID</param>
    /// <returns>The display type; null if no property with that name exists.</returns>
    public string? GetPropertyDisplayType(string propName)
    {
        PropertyInfo? prop = GetType().GetProperty(propName, _bindingFlags);
        return ItemProp.GetPropertyDisplayType(prop);
    }

    /// <summary>
    /// Get an array of all property names in this item.
    /// </summary>
    /// <returns>All property names.</returns>
    public string[] GetItemProperties()
    {
        return this.GetType().GetProperties(_bindingFlags)
            .Where(prop => prop.GetCustomAttribute<ItemProp>() != null).Select(prop => prop.Name).ToArray();
    }

    public bool HasProperty(string propName)
    {
        return this.GetType().GetProperty(propName, _bindingFlags)?.GetCustomAttribute<ItemProp>() != null;
    }

    /// <summary>
    /// Attempt to retrieve the value of a property by name. Only use for dynamic property reading; it's faster to read the property directly.
    /// </summary>
    /// <typeparam name="T">Property type.</typeparam>
    /// <param name="propName">Property name.</param>
    /// <param name="value">Variable to store value in.</param>
    /// <returns>If the property was found and is the correct type.</returns>
    public bool TryGetPropertyValue<T>(string propName, out T? value)
    {
        PropertyInfo? prop = GetType().GetProperty(propName, _bindingFlags);
        if (prop?.GetCustomAttribute<ItemProp>() == null)
        {
            value = default;
            return false;
        }

        var val = prop.GetValue(this);
        if (val is T)
        {
            value = (T)val;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    /// <summary>
    /// Get a property value as a variant. Value must be variant-compatible.
    /// </summary>
    /// <param name="propName"></param>
    /// <returns></returns>
    public Variant GetPropertyValue(string propName)
    {
        if (TryGetPropertyValue<object>(propName, out var val))
        {
            if (VariantUtils.TryCreateVariant(in val, out var variant))
            {
                return variant;
            }
            else
            {
                GD.PushWarning($"Unable to create variant from {val}. Make sure it is a variant-compatible type!");
                return default;
            }
        }
        else return default;
    }

    /// <summary>
    /// Attempt to set the value of a property by name. Only use for dynamic property writing; it's faster to set the property directly.
    /// </summary>
    /// <typeparam name="T">Type to set.</typeparam>
    /// <param name="propName">Property name.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>If the property was found and is the correct type.</returns>
    public bool TrySetPropertyValue<T>(string propName, in T value)
    {
        PropertyInfo? prop = GetType().GetProperty(propName, _bindingFlags);
        if (prop?.GetCustomAttribute<ItemProp>() == null)
        {
            return false;
        }

        if (prop.PropertyType.IsAssignableFrom(typeof(T)))
        {
            prop.SetValue(this, value);
            return true;
        }
        else return false;
    }

    /// <summary>
    /// Set the value of a property by name as a variant. Only use for dynamic property writing; it's faster to set the property directly.
    /// </summary>
    /// <param name="propName">Property name.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>If the property was found and is the correct type.</returns>
    public bool SetPropertyValue(string propName, Variant value)
    {
        return TrySetPropertyValue(propName, value.Obj);
    }

}
