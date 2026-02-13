using System.Collections.Generic;
using System.Linq;
using PuzzlemakerPlus.Items;

namespace PuzzlemakerPlus.Commands;

public class RemoveItemsCommand(IEnumerable<Item> items) : ICommand
{
    private Item[] _items = items.ToArray();

    public bool ReSelect { get; set; } = false;
    
    public bool Execute()
    {
        if (!_items.Any())
            return false;
        foreach (var item in _items)
        {
            PuzzlemakerProject.Instance.RemoveItem(item);
        }
        return true;
    }

    public void Undo()
    {
        foreach (var item in _items)
        {
            PuzzlemakerProject.Instance.AddItem(item);   
        }

        if (ReSelect)
        {
            EditorState.Instance.SelectItems(_items);
        }
    }

    public void Redo()
    {
        foreach (var item in _items)
        {
            PuzzlemakerProject.Instance.RemoveItem(item);
        }
    }
}