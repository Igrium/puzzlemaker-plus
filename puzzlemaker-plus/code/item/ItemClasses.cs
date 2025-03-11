using System;
using System.Collections.Generic;


namespace PuzzlemakerPlus.Items;

public static class ItemClasses
{
    public static IDictionary<string, Type> Classes { get; } = new Dictionary<string, Type>();

    static ItemClasses()
    {
        // Register default classes.
        Classes["Item"] = typeof(Item);
    }

    public static Type? GetItemType(string name)
    {
        if (Classes.TryGetValue(name, out var type))
            return type;
        else return Type.GetType(name);
    }

    public static Item CreateInstance(string typeName, ItemType itemType, PuzzlemakerProject project, string id)
    {
        Type type = GetItemType(typeName) ?? throw new KeyNotFoundException("Unknown item type: " + typeName);
        return (Item?)Activator.CreateInstance(type, itemType, project, id) ?? throw new Exception("Unable to instansiate item");
    }
}
