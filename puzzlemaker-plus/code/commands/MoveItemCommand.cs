using System;
using System.Collections.Generic;
using Godot;
using PuzzlemakerPlus.Items;

namespace PuzzlemakerPlus.Commands;

public class MoveItemCommand : ICommand
{
    private Item _item;
    private Vector3 _oldPos;
    private Vector3 _newPos;

    public MoveItemCommand(Item item, Vector3 newPos)
    {
        _item = item;
        _newPos = newPos;
    }

    public bool Execute()
    {
        _oldPos = _item.Position;
        _item.Position = _newPos;
        return true;
    }

    public void Redo()
    {
        _item.Position = _newPos;
    }

    public void Undo()
    {
        _item.Position = _oldPos;
    }
}
