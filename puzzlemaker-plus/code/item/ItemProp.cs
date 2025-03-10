using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PuzzlemakerPlus.Item;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ItemProp : Attribute
{
    public string? DisplayName { get; }
    public string? DisplayType { get; }

    public ItemProp(string? displayName = null, string? displayType = null)
    {
        DisplayName = displayName;
        DisplayType = displayType;
    }

    public string GetDisplayName(PropertyInfo propertyInfo)
    {
        return DisplayName ?? propertyInfo.Name;
    }

    public string GetDisplayType(PropertyInfo propertyInfo)
    {
        if (DisplayType != null)
            return DisplayType;

        Type propType = propertyInfo.PropertyType;
        if (IsIntegerType(propType))
        {
            return "Int";
        }
        else if (IsFloatType(propType))
        {
            return "Float";
        }
        else
        {
            return propType.Name;
        }
    }

    public static string? GetPropertyDisplayName(PropertyInfo? propertyInfo)
    {
        return propertyInfo?.GetCustomAttribute<ItemProp>()?.GetDisplayName(propertyInfo);
    }

    public static string? GetPropertyDisplayType(PropertyInfo? propertyInfo)
    {
        return propertyInfo?.GetCustomAttribute<ItemProp>()?.GetDisplayType(propertyInfo);
    }


    private static bool IsIntegerType(Type type)
    {
        return type == typeof(sbyte) || type == typeof(byte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong);
    }

    private static bool IsFloatType(Type type)
    {
        return type == typeof(float) || type == typeof(double) || type == typeof(decimal);
    }
}
