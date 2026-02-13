using Godot;
using PuzzlemakerPlus.Items;

namespace PuzzlemakerPlus.Commands;

/// <summary>
/// An alternate version of AddItemCommand that only applies on undo/redo.
/// </summary>
public class PlaceItemCommand(Item item, Vector3 position) : ICommand
{
    public bool Execute()
    {
        item.Position = position;
        item.PlacementMode = false;
        return true;
    }

    public void Undo()
    {
        EditorState.Instance.Project.RemoveItem(item);
    }

    public void Redo()
    {
        item.Position = position;
        EditorState.Instance.Project.AddItem(item);
    }
}