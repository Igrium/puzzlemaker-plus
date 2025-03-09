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


namespace PuzzlemakerPlus.Item;

/// <summary>
/// An item within the editor. 
/// Each instance of an item will correspond to an instance of this class; for example, if there's two buttons in the map, two Items will exist.
/// The subclass of Item that's instansiated is determined by the ItemType and the item converterType's json.
/// </summary>
public class Item
{
    /// <summary>
    /// The ItemType this item is an instance of.
    /// </summary>
    public ItemType Type { get; }

    public Item(ItemType type)
    {
        this.Type = type;
    }

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

            string serialName = itemProp.SerialName ?? prop.Name;

            if (json.TryGetPropertyValue(serialName, out var node))
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
        foreach (PropertyInfo prop in this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.FlattenHierarchy))
        {
            ItemProp? itemProp = prop.GetCustomAttribute<ItemProp>();
            if (itemProp == null)
                continue;

            string serialName = itemProp.SerialName ?? prop.Name;

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

            json[serialName] = node;
        }
    }

}
