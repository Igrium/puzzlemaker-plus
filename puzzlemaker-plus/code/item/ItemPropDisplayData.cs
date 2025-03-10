using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus.Item;

/// <summary>
/// A simple wrapper around ItemProp data to use in GDScript
/// </summary>
public partial class ItemPropDisplayData : RefCounted
{
    public string DisplayName { get; set; }
    public string DisplayType { get; set; }
    public Variant Value { get; set; } 

    public ItemPropDisplayData(string displayName, string displayType, Variant value)
    {
        DisplayName = displayName;
        DisplayType = displayType;
        Value = value;
    }

    /// <summary>
    /// Reflectively create an ItemPropDisplayData instance from a property.
    /// </summary>
    /// <param name="propertyInfo">Property info.</param>
    /// <param name="value">Value to attach to the display data.</param>
    /// <returns>The instance, or null if the property isn't an item property.</returns>
    public static ItemPropDisplayData? FromProperty(PropertyInfo propertyInfo, Variant value)
    {
        ItemProp? prop = propertyInfo.GetCustomAttribute<ItemProp>();
        if (prop == null) return null;

        return new ItemPropDisplayData(prop.GetDisplayName(propertyInfo), prop.GetDisplayType(propertyInfo), value);
    }

    public static ItemPropDisplayData? FromProperty(PropertyInfo propertyInfo, object instance)
    {
        object? value = propertyInfo.GetValue(instance);
        if (VariantUtils.TryCreateVariant(value, out var variant))
            return FromProperty(propertyInfo, variant);
        else
            return null;
    }
}
