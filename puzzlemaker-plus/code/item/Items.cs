using System;
using System.Collections.Generic;


namespace PuzzlemakerPlus.Items;

public static class Items
{
    public delegate Item ItemFactory(ItemType type);

    public static Dictionary<string, ItemFactory> ItemClasses { get; } = new();

    static Items()
    {
        // Register default classes
        ItemClasses["Item"] = (type) => new Item(type); 
    }
    
    
}
