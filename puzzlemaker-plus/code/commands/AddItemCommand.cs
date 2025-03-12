using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using PuzzlemakerPlus.Items;

namespace PuzzlemakerPlus.Commands;

public class AddItemCommand : ICommand
{
    private readonly string _itemType;
    private readonly Vector3 _position;

    // Store the item that was created for undo/redo
    private Item? _item;

    public AddItemCommand(string type, Vector3 position)
    {
        _position = position;
        _itemType = type;
    }

    public bool Execute()
    {
        ItemType? type = PackageManager.Instance.GetItemType(_itemType);
        if (type == null)
        {
            GD.PushError("Unknown item type: " + _itemType);
            return false;
        }

        _item = EditorState.Instance.Project.CreateItem(type);
        if (_item != null)
        {
            _item.Position = _position;
            return true;
        }
        else return false;
    }

    public void Redo()
    {
        if (_item != null)
        {
            EditorState.Instance.Project.AddItem(_item);
            _item.Position = _position;
        }
    }

    public void Undo()
    {
        if (_item != null)
            EditorState.Instance.Project.RemoveItem(_item);
    }
}
