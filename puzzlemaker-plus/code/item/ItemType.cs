using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzlemakerPlus.Item;

/// <summary>
/// A single item type such as a cube or a button.
/// One instance of this class will exist per item json in the package.
/// </summary>
public sealed class ItemType
{
    public string ItemClassName { get; set; } = "Item";
}
