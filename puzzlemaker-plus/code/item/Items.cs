using System;
using System.Collections.Generic;


namespace PuzzlemakerPlus.Items;

public static class Items
{
    public delegate Item ItemFactory(ItemType type, PuzzlemakerProject project);

    public static Dictionary<string, ItemFactory> ItemClasses { get; } = new();

    static Items()
    {
        // Register default classes
        ItemClasses["Item"] = (type, project) => new Item(type, project); 
    }
    
    /// <summary>
    /// Instantiate an item from its type
    /// </summary>
    /// <param name="type">Item type to use</param>
    /// <param name="project">Project to instantiate in (does not automatically add to project)</param>
    /// <param name="id">ID to give the new item</param>
    /// <returns>The new item</returns>
    /// <exception cref="InvalidOperationException">If the supplied item has a non-existant class</exception>
    public static Item CreateInstance(this ItemType type, PuzzlemakerProject project, string id = "")
    {
        if (ItemClasses.TryGetValue(type.ItemClassName, out var factory))
        {
            var item = factory(type, project);
            item.Id = id;
            return item;
        }
        else
        {
            throw new InvalidOperationException($"Unknown ItemClass '{type.ItemClassName}'");
        }
    }
}
