using System;
using System.Collections.Generic;
using Godot;
using PuzzlemakerPlus.Items;

namespace PuzzlemakerPlus.Commands;

public class MoveItemCommand : ICommand
{
    private Item _item;
    private Vector3 _oldPos;
    private Vector3 _oldRot;

    private Vector3 _newPos;
    private Vector3 _newRot;

    public MoveItemCommand(Item item, Vector3 newPos, Vector3 newRot)
    {
        _item = item;
        _newPos = newPos;
        _newRot = newRot;
    }

    public bool Execute()
    {
        _oldPos = _item.Position;
        _oldRot = _item.Rotation;
        _item.Position = _newPos;
        _item.Rotation = _newRot;
        return true;
    }

    public void Redo()
    {
        _item.Position = _newPos;
        _item.Rotation = _newRot;
    }

    public void Undo()
    {
        _item.Position = _oldPos;
        _item.Rotation = _oldRot;
    }
}
